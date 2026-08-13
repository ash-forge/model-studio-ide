using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ModelStudio.Core;

public class GgufWriter
{
    public static void SaveHeaderAndMetadata(string sourceFilePath, string targetFilePath, GgufHeader header)
    {
        using var sourceFs = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var targetFs = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write);
        using var targetWriter = new BinaryWriter(targetFs, Encoding.UTF8);

        // 1. Write GGUF Magic & Version
        targetWriter.Write(header.Magic);
        targetWriter.Write(header.Version);
        targetWriter.Write((ulong)header.Tensors.Count);
        targetWriter.Write((ulong)header.Metadata.Count);

        // 2. Write Metadata Key-Value pairs
        foreach (var kv in header.Metadata)
        {
            WriteGgufString(targetWriter, kv.Key);
            WriteGgufValue(targetWriter, kv.Value);
        }

        // 3. Write Tensor Info Records
        foreach (var tensor in header.Tensors)
        {
            WriteGgufString(targetWriter, tensor.Name);
            targetWriter.Write((uint)tensor.Dimensions.Length);
            foreach (var dim in tensor.Dimensions)
            {
                targetWriter.Write(dim);
            }
            targetWriter.Write((uint)tensor.Type);
            targetWriter.Write(tensor.Offset);
        }

        targetWriter.Flush();

        // 4. Align tensor data binary payload
        long currentPos = targetFs.Position;
        long alignment = 32; // Default GGUF alignment
        long padding = (alignment - (currentPos % alignment)) % alignment;
        for (int i = 0; i < padding; i++)
        {
            targetWriter.Write((byte)0);
        }

        // 5. Fast streaming copy of raw tensor binary payload from source file
        sourceFs.Seek(header.Tensors.Count > 0 ? (long)header.Tensors[0].Offset : currentPos, SeekOrigin.Begin);
        sourceFs.CopyTo(targetFs);

        targetFs.Flush();
    }

    private static void WriteGgufString(BinaryWriter writer, string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        writer.Write((ulong)bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteGgufValue(BinaryWriter writer, object value)
    {
        switch (value)
        {
            case byte b:
                writer.Write((uint)GgufValueType.Uint8);
                writer.Write(b);
                break;
            case sbyte sb:
                writer.Write((uint)GgufValueType.Int8);
                writer.Write(sb);
                break;
            case ushort us:
                writer.Write((uint)GgufValueType.Uint16);
                writer.Write(us);
                break;
            case short s:
                writer.Write((uint)GgufValueType.Int16);
                writer.Write(s);
                break;
            case uint ui:
                writer.Write((uint)GgufValueType.Uint32);
                writer.Write(ui);
                break;
            case int i:
                writer.Write((uint)GgufValueType.Int32);
                writer.Write(i);
                break;
            case float f:
                writer.Write((uint)GgufValueType.Float32);
                writer.Write(f);
                break;
            case bool bo:
                writer.Write((uint)GgufValueType.Bool);
                writer.Write((byte)(bo ? 1 : 0));
                break;
            case string st:
                writer.Write((uint)GgufValueType.String);
                WriteGgufString(writer, st);
                break;
            case ulong ul:
                writer.Write((uint)GgufValueType.Uint64);
                writer.Write(ul);
                break;
            case long l:
                writer.Write((uint)GgufValueType.Int64);
                writer.Write(l);
                break;
            case double d:
                writer.Write((uint)GgufValueType.Float64);
                writer.Write(d);
                break;
            case List<object> listObj:
                WriteGgufArray(writer, listObj);
                break;
            case string[] strArray:
                WriteGgufArray(writer, new List<object>(strArray));
                break;
            case List<string> strList:
                WriteGgufArray(writer, strList.ConvertAll(s => (object)s));
                break;
            default:
                writer.Write((uint)GgufValueType.String);
                WriteGgufString(writer, value?.ToString() ?? "");
                break;
        }
    }

    private static void WriteGgufArray(BinaryWriter writer, List<object> list)
    {
        writer.Write((uint)GgufValueType.Array);
        if (list.Count == 0)
        {
            writer.Write((uint)GgufValueType.String);
            writer.Write((ulong)0);
            return;
        }

        var first = list[0];
        GgufValueType elemType = first switch
        {
            byte => GgufValueType.Uint8,
            int => GgufValueType.Int32,
            uint => GgufValueType.Uint32,
            float => GgufValueType.Float32,
            bool => GgufValueType.Bool,
            string => GgufValueType.String,
            _ => GgufValueType.String
        };

        writer.Write((uint)elemType);
        writer.Write((ulong)list.Count);

        foreach (var item in list)
        {
            WriteGgufValueRaw(writer, item, elemType);
        }
    }

    private static void WriteGgufValueRaw(BinaryWriter writer, object item, GgufValueType type)
    {
        switch (type)
        {
            case GgufValueType.String:
                WriteGgufString(writer, item?.ToString() ?? "");
                break;
            case GgufValueType.Int32:
                writer.Write(Convert.ToInt32(item));
                break;
            case GgufValueType.Uint32:
                writer.Write(Convert.ToUInt32(item));
                break;
            case GgufValueType.Float32:
                writer.Write(Convert.ToSingle(item));
                break;
            case GgufValueType.Bool:
                writer.Write(Convert.ToBoolean(item) ? (byte)1 : (byte)0);
                break;
            default:
                WriteGgufString(writer, item?.ToString() ?? "");
                break;
        }
    }
}
