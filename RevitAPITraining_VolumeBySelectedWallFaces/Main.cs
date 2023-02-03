using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPITraining_VolumeBySelectedWallFaces
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            IList<Reference> selectionRef = uidoc.Selection.PickObjects(ObjectType.Face, "Выберите стены по Грани"); //список выбраных элементов

            double volumeWalls = 0;//для итогового значения
            foreach (var selectedElement in selectionRef) //перебор всех выбраных элементов в списке
            {
                var selectedElementCH = doc.GetElement(selectedElement); //выбираем елемент из выбраной ссылки?
                if (selectedElementCH is Wall)//если выбраный элемент стена
                {
                    Parameter volumeParameter = selectedElementCH.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED);//берем значение параметра по встроенному имени параметра
                    if (volumeParameter.StorageType == StorageType.Double) //проверка корректности типа?
                    {
                        volumeWalls+= UnitUtils.ConvertFromInternalUnits(volumeParameter.AsDouble(), UnitTypeId.CubicMeters);
                    }
                }
            }
            TaskDialog.Show("объем стен", volumeWalls.ToString());
            return Result.Succeeded;
        }
    }
}

