using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BooleanSplit
{
    public class BooleanSplitCommand : Command
    {
        public BooleanSplitCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static BooleanSplitCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "BoolSplits";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: start here modifying the behaviour of your command.
            // ---
            ObjRef obj1;
            Result rs1 = RhinoGet.GetOneObject("Select brep to split", false, Rhino.DocObjects.ObjectType.Brep, out obj1);
            if (rs1 != Result.Success)
                return Result.Failure;
            doc.Objects.UnselectAll();

            ObjRef[] obj2;
            Result rs2 = RhinoGet.GetMultipleObjects("Select multiple breps to BooleanSplit", false, ObjectType.Brep, out obj2);
            if (rs2 != Result.Success || obj2 == null)
                return Result.Failure;
            doc.Objects.UnselectAll();

            Brep brep = obj1.Brep();
            List<Brep> splitted = new List<Brep>();
            List<Brep> cutters = new List<Brep>();
            for(int i = 0; i<obj2.Length;i++)
            {
                cutters.Add(obj2[i].Brep());
            }

            splitted = boolSplits(brep, cutters);

            foreach(var item in splitted)
            {
                doc.Objects.AddBrep(item);
            }
            doc.Objects.Delete(obj1, true);
            doc.Views.Redraw();
            
            RhinoApp.WriteLine($"Splitted into {splitted.Count} breps");

            // ---
            return Result.Success;
        }

        public List<Brep> boolSplits(Brep brepsToSplit, List<Brep> Cutters)
        {
            List<Brep> splitted = new List<Brep>();
            List<Brep> resultOneStep = new List<Brep>();
            List<Brep> result = new List<Brep>();
            for (int i = 0; i < Cutters.Count; i++)
            {
                if(i == 0)
                {
                    splitted = boolSplitsSingle(brepsToSplit, Cutters[i]);
                }
                else
                {
                    for(int p = 0; p < splitted.Count; p++)
                    {
                        resultOneStep = boolSplitsSingle(splitted[i], Cutters[i]);
                        result.AddRange(resultOneStep);
                        resultOneStep.Clear();
                    }
                    splitted = result;
                }
                result.Clear();
            }
            return splitted.ToList();
        }

        public List<Brep> boolSplitsSingle(Brep brepsToSplit, Brep Cutter)
        {
            //Flip given cutter
            Brep cutterFlipped = Cutter.DuplicateBrep();
            cutterFlipped.Flip();

            Brep[] splitDir1;
            Brep[] splitDir2;
            List<Brep> Merged = new List<Brep>();
            splitDir1 = Brep.CreateBooleanDifference(brepsToSplit, Cutter, absTol);
            splitDir2 = Brep.CreateBooleanDifference(brepsToSplit, cutterFlipped, absTol);
            Merged.AddRange(splitDir1.ToList());
            Merged.AddRange(splitDir2.ToList());
            return Merged;
        }

        public double absTol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

        public static void RemoveAt(Brep[] Arr, int index)
        {
            List<Brep> list = new List<Brep>();
            list.RemoveAt(index);
            list.ToArray();
        }
    }
}
