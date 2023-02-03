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

namespace RevitAPITraining_TotalLengthByPipes
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            IList<Reference> selectionRef = uidoc.Selection.PickObjects(ObjectType.Element, "Выберите трубы"); //список выбраных элементов

            double lengthPipes = 0;//для итогового значения
            foreach (var selectedElement in selectionRef) //перебор всех выбраных элементов в списке
            {
                var selectedElementCH = doc.GetElement(selectedElement); //выбираем елемент из выбраной ссылки? ссылка становится элементом
                if (selectedElementCH is Pipe)//если выбраный элемент труба
                {
                    Parameter lengthParameter = selectedElementCH.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);//берем значение параметра по встроенному имени параметра
                    if (lengthParameter.StorageType == StorageType.Double) //проверка корректности типа
                    {
                        lengthPipes += UnitUtils.ConvertFromInternalUnits(lengthParameter.AsDouble(), UnitTypeId.Millimeters);
                    }
                }
                else continue;
            }
            TaskDialog.Show("Длина Труб", lengthPipes.ToString());
            return Result.Succeeded;
        }
    }
}
