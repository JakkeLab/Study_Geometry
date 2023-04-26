using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace IntersectionTest
{
    public class IntersectionTestCommand : Command
    {
        public IntersectionTestCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static IntersectionTestCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "isCrvsIntersect";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Select two curves to intersect
            var go = new Rhino.Input.Custom.GetObject();
            go.SetCommandPrompt("Select two curves");
            go.GeometryFilter = Rhino.DocObjects.ObjectType.Brep;
            go.GetMultiple(2, 2);
            if (go.CommandResult() != Rhino.Commands.Result.Success)
                return go.CommandResult();

            // Validate input
            var BrepA = go.Object(0).Brep();
            var BrepB = go.Object(1).Brep();
            if (BrepA == null || BrepB == null)
                return Rhino.Commands.Result.Failure;

            // Calculate the intersection
            var events = Rhino.Geometry.Intersect.Intersection.BrepBrep(BrepA, BrepB, 0.000, out Curve[] crvs, out Point3d[] pts);

            // Process the results
            RhinoApp.WriteLine($"Intersect : {events}");
            // ---
            return Result.Success;
        }
    }
}
