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
  private void RunScript(List<Curve> crvsToCut, List<Point3d> ptsForCut, ref object splittedCrvs, ref object ptsOnEnd)
  {
        splittedCrvs = GetCrvSegmentsWithinPoints(crvsToCut, ptsForCut);
    ptsOnEnd = PointsOnMultipleCurveEnd(crvsToCut, ptsForCut);
  }

  // <Custom additional code> 
    
  //Segments의 tValue 구하기
  public List<double> crvSplitTvalues(Curve crv, List<Point3d> pts)
  {
    List<double> tValues = new List<double>();
    foreach(Point3d pt in pts)
    {
      double tValue;
      crv.ClosestPoint(pt, out tValue);
      Point3d tPoint = crv.PointAt(tValue);
      if(tPoint.DistanceTo(pt) <= 0.1)
      {
        tValues.Add(tValue);
      }
    }
    tValues.Sort();
    return tValues;
  }

  //tValues에서 특정 구간의 커브 구하기
  public List<Curve> SplitCrvByTvalues(Curve crv, List<double> tValues)
  {
    List<Curve> result = new List<Curve>();
    tValues.Sort();
    List<double> sortedTvalues = new List<double>();
    sortedTvalues.AddRange(tValues);

    for(int i = 0; i < tValues.Count - 1; i++)
    {
      Curve segmentCrv = crv.Trim(sortedTvalues[i], sortedTvalues[i + 1]);
      result.Add(segmentCrv);
    }

    return result;
  }

  //다중커브 대응
  public List<Curve> GetCrvSegmentsWithinPoints(List<Curve> crvs, List<Point3d> pts)
  {
    List<Curve> segments = new List<Curve>();
    foreach(Curve crv in crvs)
    {
      var tValues = crvSplitTvalues(crv, pts);
      var splittedOnSingleCrv = SplitCrvByTvalues(crv, tValues);
      segments.AddRange(splittedOnSingleCrv);
    }

    return segments;
  }

  //맨 바깥 포인트 찾기
  public List<Point3d> PointsOnCurveEnd(Curve crv, List<double> tValues)
  {
    List<Point3d> result = new List<Point3d>();
    double t1 = tValues.First();
    double t2 = tValues.Last();
    result.Add(crv.PointAt(t1));
    result.Add(crv.PointAt(t2));
    return result;
  }

  //맨 바깥포인트 찾기 다중커브 대응
  public List<Point3d> PointsOnMultipleCurveEnd(List<Curve> crvs, List<Point3d> pts)
  {
    List<Point3d> result = new List<Point3d>();
    foreach(Curve crv in crvs)
    {
      List<double> tValues = crvSplitTvalues(crv, pts);
      List<Point3d> endPts = PointsOnCurveEnd(crv, tValues);
      result.AddRange(endPts);
    }
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
        List<Curve> crvsToCut = null;
    if (inputs[0] != null)
    {
      crvsToCut = GH_DirtyCaster.CastToList<Curve>(inputs[0]);
    }
    List<Point3d> ptsForCut = null;
    if (inputs[1] != null)
    {
      ptsForCut = GH_DirtyCaster.CastToList<Point3d>(inputs[1]);
    }


    //3. Declare output parameters
      object splittedCrvs = null;
  object ptsOnEnd = null;


    //4. Invoke RunScript
    RunScript(crvsToCut, ptsForCut, ref splittedCrvs, ref ptsOnEnd);
      
    try
    {
      //5. Assign output parameters to component...
            if (splittedCrvs != null)
      {
        if (GH_Format.TreatAsCollection(splittedCrvs))
        {
          IEnumerable __enum_splittedCrvs = (IEnumerable)(splittedCrvs);
          DA.SetDataList(0, __enum_splittedCrvs);
        }
        else
        {
          if (splittedCrvs is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(0, (Grasshopper.Kernel.Data.IGH_DataTree)(splittedCrvs));
          }
          else
          {
            //assign direct
            DA.SetData(0, splittedCrvs);
          }
        }
      }
      else
      {
        DA.SetData(0, null);
      }
      if (ptsOnEnd != null)
      {
        if (GH_Format.TreatAsCollection(ptsOnEnd))
        {
          IEnumerable __enum_ptsOnEnd = (IEnumerable)(ptsOnEnd);
          DA.SetDataList(1, __enum_ptsOnEnd);
        }
        else
        {
          if (ptsOnEnd is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(ptsOnEnd));
          }
          else
          {
            //assign direct
            DA.SetData(1, ptsOnEnd);
          }
        }
      }
      else
      {
        DA.SetData(1, null);
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