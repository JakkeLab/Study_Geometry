using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BooleanSplit
{
    public class BooleanSplitCommand2 : Command
    {
        public BooleanSplitCommand2()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static BooleanSplitCommand2 Instance { get; private set; }

        public override string EnglishName => "BoolSplitsSingle";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoGet.GetOneObject("Please pick one brep to split", false, ObjectType.Brep, out ObjRef obj1);
            Brep brep1 = obj1.Brep();
            doc.Objects.UnselectAll();

            RhinoGet.GetOneObject("Please pick one brep to split", false, ObjectType.Brep, out ObjRef obj2);
            Brep cutter = obj2.Brep();
            doc.Objects.UnselectAll();
            // ---
            List<Brep> splitted = boolSplitsSingle(brep1, cutter);
            doc.Objects.Delete(doc.Objects.Find(obj1.ObjectId));
            foreach(Brep item in splitted)
            {
                doc.Objects.AddBrep(item);
                doc.Views.Redraw();
            }
            return Result.Success;
        }
        public double absTol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
        public List<Brep> boolSplitsSingle(Brep brepsToSplit, Brep Cutter)
        {
            //Flip given cutter
            Brep cutterFlipped = Cutter.DuplicateBrep();;
            cutterFlipped.Flip();

            Brep[] splitDir1;
            Brep[] splitDir2;
            List<Brep> Merged = new List<Brep>();
            if(Intersection.BrepBrep(brepsToSplit, Cutter, 0, out Curve[] intCrv1, out Point3d[] intPts1))
            {
                splitDir1 = Brep.CreateBooleanDifference(brepsToSplit, Cutter, absTol, true);
                Merged.AddRange(splitDir1.ToList());
            }
            if (Intersection.BrepBrep(brepsToSplit, Cutter, 0, out Curve[] intCrv2, out Point3d[] intPts2))
            {
                splitDir2 = Brep.CreateBooleanDifference(brepsToSplit, cutterFlipped, absTol, true);
                Merged.AddRange(splitDir2.ToList());
            }

            return Merged;
        }

        
    }
}