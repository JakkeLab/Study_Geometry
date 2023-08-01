using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.Linq;

/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance
{
#region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { __out.Add(text); }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { __out.Add(string.Format(format, args)); }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { __out.Add(GH_ScriptComponentUtilities.ReflectType_CS(obj)); }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { __out.Add(GH_ScriptComponentUtilities.ReflectType_CS(obj, method_name)); }
#endregion

#region Members
  /// <summary>Gets the current Rhino document.</summary>
  private RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private IGH_Component Component; 
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private int Iteration;
#endregion

  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments, 
  /// Output parameters as ref arguments. You don't have to assign output parameters, 
  /// they will have a default value.
  /// </summary>
  private void RunScript(List<Point3d> pts, List<Point3d> startPt, List<Curve> segments, ref object A)
  {
        A = GetCCWCycle(startPt[0], segments);
  }

  // <Custom additional code> 
    public double MinDist(Point3d target, List<Point3d> comparables)
  {
    var dists = comparables.Select(x => x.DistanceTo(target)).ToList();
    return dists.Min();
  }

  //인접노드 구하기
  public List<Point3d> GetLinkedNodes(Point3d target, List<Curve> segments)
  {
    List<Point3d> result = new List<Point3d>();
    foreach(Curve segment in segments)
    {
      Point3d st = segment.PointAtStart;
      Point3d ed = segment.PointAtEnd;
      if(target.DistanceTo(st) < 0.5 && target.DistanceTo(ed) > 0.5)
      {
        result.Add(ed);
      }
      else if(target.DistanceTo(st) > 0.5 && target.DistanceTo(ed) < 0.5)
      {
        result.Add(st);
      }
    }
    return result;
  }

  //인접노드 반시계 정렬
  public List<Point3d> SortLinkedNodesByCCW(Point3d before, Point3d current, List<Point3d> targets, bool isFirst = false)
  {
    List<Point3d> result = new List<Point3d>();
    List<object[]> polarList = new List<object[]>();
    if(targets.Count() > 1)
    {
      foreach(Point3d target in targets)
      {
        Vector3d polarAxis = new Vector3d();
        if(isFirst)
        {
          polarAxis = Vector3d.XAxis;
        }
        else
        {
          polarAxis = new Vector3d(current - before);
        }
        object[] polarCoord = GetPolarCoorinate(target, current, polarAxis);
        polarList.Add(polarCoord);
      }
      var orderedPts = polarList.OrderBy(x => (double) x[3]).ToList();
      result = orderedPts.Select(x => (Point3d) x[1]).ToList();
    }
    else
    {
      result = targets;
    }
    return result;
  }

  public object[] GetPolarCoorinate(Point3d pt, Point3d polarOrigin, Vector3d axis)
  {
    Vector3d axisToPt = new Vector3d
      (pt.X - polarOrigin.X, pt.Y - polarOrigin.Y, pt.Z - polarOrigin.Z);
    int crossVec = 1;
    if(Vector3d.CrossProduct(axis, axisToPt).Z > 0)
    {
      crossVec = -1;
    }
    double angleToPt = Vector3d.VectorAngle(axis, axisToPt) * crossVec;
    double distToPt = polarOrigin.DistanceTo(pt);
    return new object[4] {"PolarCoord", (Point3d) pt ,(double) distToPt, (double) angleToPt };
  }

  //CCW 순회
  public List<Point3d> GetCCWCycle(Point3d startPt, List<Curve> segments)
  {
    List<Point3d> result = new List<Point3d>();
    List<Point3d> cycles = new List<Point3d>();
    cycles.Add(startPt);
    List<Point3d> nodes = new List<Point3d>();
    //첫번째 탐색
    List<Point3d> firstNodes = GetLinkedNodes(startPt, segments);
    var firstResult = SortLinkedNodesByCCW(Point3d.Origin, startPt, firstNodes, true);
    cycles.Add(firstResult[0]);
    bool firstPtEncountered = false;
    //2번째 ~ 마지막 탐색
    while(!firstPtEncountered)
    {
      List<Point3d> linkedNodes = GetLinkedNodes(cycles.Last(), segments);
      var sortResult = SortLinkedNodesByCCW(cycles[cycles.Count() - 2], cycles[cycles.Count() - 1], linkedNodes);
      var sortFiltered = new List<Point3d>();
      foreach(Point3d pt in sortResult)
      {
        if(MinDist(pt, cycles) > 0.5)
        {
          sortFiltered.Add(pt);
        }
      }
      cycles.Add(sortFiltered[0]);

      //마지막에 시작점 노드가 인접노드로 발견되는지 확인
      var lastAdjacentNodes = GetLinkedNodes(cycles.Last(), segments);
      foreach(Point3d pt in lastAdjacentNodes)
      {
        if(MinDist(cycles.First(), lastAdjacentNodes) < 0.5)
        {
          firstPtEncountered = true;
        }
      }
    }
    result = cycles;
    return result;
  }

  // </Custom additional code> 

  private List<string> __err = new List<string>(); //Do not modify this list directly.
  private List<string> __out = new List<string>(); //Do not modify this list directly.
  private RhinoDoc doc = RhinoDoc.ActiveDoc;       //Legacy field.
  private IGH_ActiveObject owner;                  //Legacy field.
  private int runCount;                            //Legacy field.
  
  public override void InvokeRunScript(IGH_Component owner, object rhinoDocument, int iteration, List<object> inputs, IGH_DataAccess DA)
  {
    //Prepare for a new run...
    //1. Reset lists
    this.__out.Clear();
    this.__err.Clear();

    this.Component = owner;
    this.Iteration = iteration;
    this.GrasshopperDocument = owner.OnPingDocument();
    this.RhinoDocument = rhinoDocument as Rhino.RhinoDoc;

    this.owner = this.Component;
    this.runCount = this.Iteration;
    this. doc = this.RhinoDocument;

    //2. Assign input parameters
        List<Point3d> pts = null;
    if (inputs[0] != null)
    {
      pts = GH_DirtyCaster.CastToList<Point3d>(inputs[0]);
    }
    List<Point3d> startPt = null;
    if (inputs[1] != null)
    {
      startPt = GH_DirtyCaster.CastToList<Point3d>(inputs[1]);
    }
    List<Curve> segments = null;
    if (inputs[2] != null)
    {
      segments = GH_DirtyCaster.CastToList<Curve>(inputs[2]);
    }


    //3. Declare output parameters
      object A = null;


    //4. Invoke RunScript
    RunScript(pts, startPt, segments, ref A);
      
    try
    {
      //5. Assign output parameters to component...
            if (A != null)
      {
        if (GH_Format.TreatAsCollection(A))
        {
          IEnumerable __enum_A = (IEnumerable)(A);
          DA.SetDataList(0, __enum_A);
        }
        else
        {
          if (A is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(0, (Grasshopper.Kernel.Data.IGH_DataTree)(A));
          }
          else
          {
            //assign direct
            DA.SetData(0, A);
          }
        }
      }
      else
      {
        DA.SetData(0, null);
      }

    }
    catch (Exception ex)
    {
      this.__err.Add(string.Format("Script exception: {0}", ex.Message));
    }
    finally
    {
      //Add errors and messages... 
      if (owner.Params.Output.Count > 0)
      {
        if (owner.Params.Output[0] is Grasshopper.Kernel.Parameters.Param_String)
        {
          List<string> __errors_plus_messages = new List<string>();
          if (this.__err != null) { __errors_plus_messages.AddRange(this.__err); }
          if (this.__out != null) { __errors_plus_messages.AddRange(this.__out); }
          if (__errors_plus_messages.Count > 0) 
            DA.SetDataList(0, __errors_plus_messages);
        }
      }
    }
  }
}