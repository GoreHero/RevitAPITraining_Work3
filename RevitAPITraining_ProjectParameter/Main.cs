using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using Transaction = Autodesk.Revit.DB.Transaction;

namespace RevitAPITraining_ProjectParameter
{
    [Transaction(TransactionMode.Manual)]

    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            //создание параметра
            var categorySet = new CategorySet();
            categorySet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_PipeCurves));
            //транзакция => создание 
            using (Transaction ts = new Transaction(doc, "Add parameter"))
            {
                ts.Start();
                CreateSharedParameter(uiapp.Application, doc, "Наименование", categorySet, BuiltInParameterGroup.PG_DATA, true);
                ts.Commit();
            }
            //собираем все трубы в файле
            List<Pipe> pipes = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_PipeCurves)
                .WhereElementIsNotElementType()
                .Cast<Pipe>()
                .ToList();
            //проходимся по списку труб из файла
            foreach (var elem in pipes)
            {
                Parameter pName = elem.LookupParameter("Наименование"); //выбираем параметр определенного элемента
                Parameter outerD = elem.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER); //берем значение параметра по встроенному имени параметра
                Parameter innerD = elem.get_Parameter(BuiltInParameter.RBS_PIPE_INNER_DIAM_PARAM);//берем значение параметра по встроенному имени параметра

                string outerDstr = outerD.AsValueString();
                string innerDstr = innerD.AsValueString();
                string newName = $"Труба {outerDstr}/{innerDstr}";
                //запись результата в параметр
                using (Transaction ts = new Transaction(doc, "Set parameter"))
                {
                    ts.Start();
                    pName.Set(newName);
                    ts.Commit();
                }
            }
            return Result.Succeeded;
        }
        /*********************************/
        private void CreateSharedParameter(
            Application application,
            Document doc,
            string parameterName,
            CategorySet categorySet,
            BuiltInParameterGroup builtInParameterGroup,
            bool isInstance)
        {
            DefinitionFile definitionFile = application.OpenSharedParameterFile();
            if (definitionFile == null)
            {
                TaskDialog.Show("Ошибка", "Не найден файл общих параметров");
                return;
            }

            Definition definition = definitionFile.Groups
                .SelectMany(group => group.Definitions)
                .FirstOrDefault(def => def.Name.Equals(parameterName));
            if (definition == null)
            {
                TaskDialog.Show("Ошибка", "Не найден указанный параметр");
                return;
            }

            Binding binding = application.Create.NewTypeBinding(categorySet);
            if (isInstance)
                binding = application.Create.NewInstanceBinding(categorySet);

            BindingMap map = doc.ParameterBindings;
            map.Insert(definition, binding, builtInParameterGroup);
        }
    }
}