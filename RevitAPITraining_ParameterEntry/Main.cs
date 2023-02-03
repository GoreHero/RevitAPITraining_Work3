using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPITraining_ParameterEntry
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            //************************Создание параметра****************
            var categorySet = new CategorySet();
            categorySet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_PipeCurves));
            using (Transaction ts = new Transaction(doc, "Add parameter"))//транзакция
            {
                ts.Start();
                CreateSharedParametr(uiapp.Application, doc, "Размер с запасом", categorySet, BuiltInParameterGroup.PG_DATA, true);//создание метода
                //                  uiapp.Application добраться до общих параметров,
                //                 doc куда добавить параметр,
                //                 "TestParametr" название добавляемого параметра,
                //                categorySet к каким именно категориям добавляетс параметр
                //                 BuiltInParameterGroup.PG_DATA к каким группа будет добавлен параметр,
                //                 true добавляемый параметр типа или экземпляра?

                ts.Commit();
            }

            //************************Запись значения в параметр****************
            int countI = 0;
            IList<Reference> selectionRef = uidoc.Selection.PickObjects(ObjectType.Element, "Выберите трубы"); //список выбраных элементов
            foreach (var selectedElement in selectionRef)
            {
                var selectedElementCH = doc.GetElement(selectedElement); //обращение к выбранному элементу
                if (selectedElementCH is Pipe)//если выбраный элемент труба
                {
                    Parameter lengthParameter = selectedElementCH.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);//берем значение параметра по встроенному имени параметра
                    if (lengthParameter.StorageType == StorageType.Double) //проверка корректности типа
                    {
                        //double lengthPipe = (UnitUtils.ConvertFromInternalUnits(lengthParameter.AsDouble(), UnitTypeId.Millimeters)) * 1.1;
                        
                        Element element = doc.GetElement(selectedElement);

                        var elementPipe = element as Pipe;
                        using (Transaction ts = new Transaction(doc, "set parametrs"))
                        {
                            ts.Start();
                            Parameter zapasParameter = elementPipe.LookupParameter("Размер с запасом"); //выбор параметра выбранного элемента                             
                            zapasParameter.Set(Convert.ToString((UnitUtils.ConvertFromInternalUnits((lengthParameter.AsDouble()) * 1.1, UnitTypeId.Meters))));//запись значения в данный параметр 
                            
                            //Parameter typeComentsParametr = familyInstance.Symbol.LookupParameter("Type Comments"); //выбор параметра типа//нужно обратиться к свойству Symbol переменной фэмилиИнстанс,найти параметр
                            //typeComentsParametr.Set("TestTypeComments");//запись значения
                            ts.Commit();
                        }
                        countI++;
                    }
                }
                else continue;
            }
            TaskDialog.Show("Готово", $"количество изменений {countI}");
            return Result.Succeeded;
        }
        //*****************************************************************************************

        private void CreateSharedParametr(Application application,
            Document doc, string parametrName, CategorySet categorySet,
            BuiltInParameterGroup builtInParameterGroup, bool isInstance)
        {
            DefinitionFile definitionFile = application.OpenSharedParameterFile(); //добираемся до файла общих параметров
            if (definitionFile == null) //проверка задан ли параметр
            {
                TaskDialog.Show("Ошибка", "Не найден файл общих параметров");
                return;
            }
            ///////добрались до параметров
            //поиск определения параметра
            Definition definition = definitionFile.Groups //открываем файл общих параметров, выбираем все группы,
                .SelectMany(Group => Group.Definitions) //SelectMany=>выбираем все определения параметров
                .FirstOrDefault(def => def.Name.Equals(parametrName));  //FirstOrDefault выбираем конкретное определение параметров
            if (definition == null)//проверка есть ли определение параметра по имени
            {
                TaskDialog.Show("Ошибка", "не найден указанный параметр");
                return;
            }
            ////////добрались до определения параметра
            Binding binding = application.Create.NewTypeBinding(categorySet); //если параметр типа, создаем параметр
            if (isInstance)
                binding = application.Create.NewInstanceBinding(categorySet);//если параметр экземпляра
            BindingMap map = doc.ParameterBindings; //вставляем новый параметр
            map.Insert(definition, binding, builtInParameterGroup);
        }
    }
}
