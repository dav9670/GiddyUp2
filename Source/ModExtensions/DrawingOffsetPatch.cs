using GiddyUp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace GiddyUp
{
    class DrawingOffsetPatch : DefModExtension
    {
        //Can be used in xml patches to apply custom offsets to how riders are drawn on their animals. 
     
        string northOffsetCSV = "";
        string southOffsetCSV = "";
        string eastOffsetCSV = "";
        string westOffsetCSV = "";
        public Vector3 northOffset;
        public Vector3 southOffset;
        public Vector3 eastOffset;
        public Vector3 westOffset;

        //Since it is used for drawing pawns, it is expected to be called VERY frequently. Therefore by initting this instead of converting on the fly, possible impact on performance is reduced. 
        public void Init()
        {
            northOffset = TextureUtility.ExtractVector3(northOffsetCSV);
            southOffset = TextureUtility.ExtractVector3(southOffsetCSV);
            eastOffset = TextureUtility.ExtractVector3(eastOffsetCSV);
            westOffset = TextureUtility.ExtractVector3(westOffsetCSV);
        }



    }
}
