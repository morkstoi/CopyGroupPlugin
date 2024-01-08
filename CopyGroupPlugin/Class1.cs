using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CopyGroup : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            try
            {
                UIDocument uIDoc = commandData.Application.ActiveUIDocument;
                Document doc = uIDoc.Document;

                GroupPickFilter groupPickFilter = new GroupPickFilter();
                Reference reference = uIDoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберите группу объектов");
                Element element = doc.GetElement(reference);
                Group group = element as Group;
                XYZ groupCenter = GetElementCenter(group);
                Room room = GetRoomByPoint(doc, groupCenter);
                XYZ roomCenter = GetElementCenter(room);
                XYZ offset = groupCenter - roomCenter;


                XYZ point = uIDoc.Selection.PickPoint("Выберите точку");
                Room oRoom = GetRoomByPoint(doc, point);
                XYZ oRoomCenter = GetElementCenter(oRoom);
                XYZ pointTemp = oRoomCenter - offset;
                LocationPoint locationPoint = group.Location as LocationPoint;
                XYZ endPoint = new XYZ(pointTemp.X, pointTemp.Y, locationPoint.Point.Z);



                using (Transaction transaction = new Transaction(doc))
                {
                    transaction.Start("Копирование группы объектов");
                    doc.Create.PlaceGroup(point, group.GroupType);


                    transaction.Commit();

                }

            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {

                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;

            }

            return Result.Succeeded;

        }

        public XYZ GetElementCenter(Element element)
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;

        }

        public Room GetRoomByPoint(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Rooms);
            foreach (Element e in collector)
            {
                Room room = e as Room;
                if (room != null)
                {
                    if (room.IsPointInRoom(point))
                    {
                        return room;
                    }
                }

            }
            return null;
        }


    }

    public class GroupPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }


}
