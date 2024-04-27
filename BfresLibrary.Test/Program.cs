using BfresLibrary;
using BfresLibrary.Helpers;
using BfresLibrary.Switch;
using System;
using System.Collections;
using System.Collections.Generic;
using BfresLibrary.GX2;
using Syroot.Maths;
using System.Drawing;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {

            string modelName = "Model.bfres";
            if (args.Length >= 1) modelName = args[0];

            var resFile = new ResFile(modelName);
            LoadMeshData(resFile);

            resFile.Save("FixedModel.bfres");
        }

        static void LoadMeshData(ResFile resFile)
        {
            foreach (var model in resFile.Models.Values)
            {
                foreach (var shape in model.Shapes.Values)
                {
                    var material = model.Materials[shape.MaterialIndex];
                    var hasPaint = false;
                    foreach (var att in material.ShaderAssign.ShaderOptions) {

                        if (att.Key == "blitz_paint_type" && att.Value != "<Default Value>" && att.Value != "0")
                        {
                            hasPaint = true;
                            break;
                        }
                    }
                    if (!hasPaint) continue;

                    var vertexBuffer = model.VertexBuffers[shape.VertexBufferIndex];

                    //Vertex data
                    VertexBufferHelper helper = new VertexBufferHelper(vertexBuffer, resFile.ByteOrder);
                    byte count = (byte)helper.Attributes.Count;

                    string[] names = { "_pu0", "_pu1", "_pu2" };

                    bool needsFix = true;
                    foreach (var att in helper.Attributes)
                    {
                        if (att.Name == names[0] || att.Name == names[1] || att.Name == names[2])
                        {
                            needsFix = false;
                            break;
                        }
                    }

                    if (!needsFix) continue;

                    var stIdx = 0;
                    for(var i = 0; i < count; i++)
                    {
                        var name = helper.Attributes[i].Name;
                        var at = helper.Attributes[i].BufferIndex;
                        if (name == "_p0" || name == "_n0" || name == "_t0" || name == "_u0" || name == "_u1" || name == "_c0") 
                            stIdx = Math.Max(stIdx, at + 1);
                    }

                    foreach (var att in helper.Attributes)
                    {
                        if (att.BufferIndex >= stIdx) att.BufferIndex++;
                    }

                    uint[] offsets = { 0, 12, 8 };
                    Vector4F[] pData = { new Vector4F(1, 1, 1, 1), new Vector4F(1, 0, 0, 0), new Vector4F(1, 1, 1, -1) };
                    GX2AttribFormat[] formats = { GX2AttribFormat.Format_16_16_16_16_SNorm, GX2AttribFormat.Format_8_SNorm, GX2AttribFormat.Format_10_10_10_2_SNorm };
                    for (var i = 0; i < 3; i++) {
                        helper.Attributes.Add(new VertexBufferHelperAttrib());
                        helper.Attributes[count + i].Name = names[i];
                        helper.Attributes[count + i].Data = new Vector4F[helper.Attributes[0].Data.Length];
                        for (var j = 0; j < helper.Attributes[0].Data.Length; j++) helper.Attributes[count + i].Data[j] = pData[i];
                        helper.Attributes[count + i].BufferIndex = (Byte)stIdx;
                        helper.Attributes[count + i].Format = formats[i];
                        helper.Attributes[count + i].Offset = offsets[i];
                        helper.Attributes[count + i].Stride = 0;
                    }

                    /*
                    foreach (var att in helper.Attributes)
                    {
                        Console.WriteLine("{0} {1} {2} {3} {4} {5}", att.Name, att.BufferIndex, att.Format, att.Offset, att.stride, att.Data.Length);
                        if (att.Name != "_pu0" && att.Name != "_pu1" && att.Name != "_pu2") continue;
                        Console.WriteLine("{0} {1} {2} {3}", att.Data[0].X, att.Data[1].Y, att.Data[2].Z, att.Data[3].W);
                        
                    }
                    */
                    model.VertexBuffers[shape.VertexBufferIndex] = helper.ToVertexBuffer();
                }
            }
        }
    }
}
