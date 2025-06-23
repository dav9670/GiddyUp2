using UnityEngine;
using Verse;

namespace GiddyUp;

//For ThingDefs (races)
internal class DrawingOffset : DefModExtension
{
    //Can be used in xml patches to apply custom offsets to how riders are drawn on their animals. 
    public Vector3 northOffset = Vector3.zero;
    public Vector3 southOffset = Vector3.zero;
    public Vector3 eastOffset = Vector3.zero;
    public Vector3 westOffset = Vector3.zero;
}