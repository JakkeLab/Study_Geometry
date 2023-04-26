using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;



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
  private void RunScript(List<Point3d> Pts, double Dis, Curve SortAlong, bool Dir, ref object Res)
  {
        Res = GetOffsetPoints(Pts, Dis, Dir);
  }

  // <Custom additional code> 
    //포인트 오프셋
  public List<Point3d> GetOffsetPoints(List<Point3d> pts, double dist, bool offsetInner)
  {
    List<Point3d> result = new List<Point3d>();
    double thr = Math.PI / 180;
    int dirConst = offsetInner == true ? -1 : 1;
    for(int i = 0; i < pts.Count - 1; i++)
    {
      if(i == 0)
      {
        Point3d P1 = pts[0];
        Point3d P2 = pts[1];
        result.Add(RotatePoint(P1, P2, dirConst * Math.PI * 0.5, dist));
      }
      else if (i < pts.Count - 1)
      {
        Point3d P1 = pts[i - 1];
        Point3d P2 = pts[i];
        Point3d P3 = pts[i + 1];

        if(IsCCW(P1, P2, P3, thr))
        {
          result.Add(RotatePoint(P2, P3, dirConst * Math.PI * 0.5, dist));
        }
        else
        {
          result.Add(RotatePoint(P2, P3, -dirConst * Math.PI * 0.5, dist));
        }
      }
      else
      {
        Point3d P1 = pts[i - 1];
        Point3d P2 = pts[i];
        Point3d P3 = pts[0];
        if(IsCCW(P1, P2, P3, thr))
        {
          result.Add(RotatePoint(P2, P3, dirConst * Math.PI * 0.5, dist));
        }
        else
        {
          result.Add(RotatePoint(P2, P3, -dirConst * Math.PI * 0.5, dist));
        }
      }
    }
    return result;
  }

  //CCW 알고리즘
  public bool IsCCW(Point3d P1, Point3d P2, Point3d P3, double threshould)
  {
    Vector3d P1P2 = P2 - P1;
    Vector3d P2P3 = P3 - P2;

    Vector3d cross = Vector3d.CrossProduct(P1P2, P2P3);
    if(Vector3d.VectorAngle(P1P2, P2P3) < threshould)
    {
      return true;
    }
    else
    {
      return cross.Z > 0;
    }
  }

  //P2를 P1을 중심으로 rad만큼 회전시키고 원하는 길이만큼 보내기
  public Point3d RotatePoint(Point3d P1, Point3d P2, double rad, double dist)
  {
    //P1를 원점에 옮기는 만큼 P2를 옮기기
    Vector3d P2Originated = P2 - P1;

    //회전변환 실행
    double newXOriginated = Math.Cos(rad) * P2Originated.X - Math.Sin(rad) * P2Originated.Y;
    double newYOriginated = Math.Sin(rad) * P2Originated.X + Math.Cos(rad) * P2Originated.Y;

    Vector3d newPointVector = new Vector3d(newXOriginated, newYOriginated, 0);

    //단위벡터화
    newPointVector.Unitize();

    //원하는 거리만큼 이동
    Point3d distPoint = new Point3d(dist * newPointVector.X, dist * newPointVector.Y, 0);


    //기존에 원점으로 옮긴만큼 다시 옮겨주기
    Point3d newPtOriginated = new Point3d(distPoint.X + P1.X, distPoint.Y + P1.Y, 0);

    return newPtOriginated;
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
        List<Point3d> Pts = null;
    if (inputs[0] != null)
    {
      Pts = GH_DirtyCaster.CastToList<Point3d>(inputs[0]);
    }
    double Dis = default(double);
    if (inputs[1] != null)
    {
      Dis = (double)(inputs[1]);
    }

    Curve SortAlong = default(Curve);
    if (inputs[2] != null)
    {
      SortAlong = (Curve)(inputs[2]);
    }

    bool Dir = default(bool);
    if (inputs[3] != null)
    {
      Dir = (bool)(inputs[3]);
    }



    //3. Declare output parameters
      object Res = null;


    //4. Invoke RunScript
    RunScript(Pts, Dis, SortAlong, Dir, ref Res);
      
    try
    {
      //5. Assign output parameters to component...
            if (Res != null)
      {
        if (GH_Format.TreatAsCollection(Res))
        {
          IEnumerable __enum_Res = (IEnumerable)(Res);
          DA.SetDataList(0, __enum_Res);
        }
        else
        {
          if (Res is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(0, (Grasshopper.Kernel.Data.IGH_DataTree)(Res));
          }
          else
          {
            //assign direct
            DA.SetData(0, Res);
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