using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Communications.MetaData
{
    // данные имеют свой тип и набор атрибутов  
    // элементы метаданных (byte)
    // |  7  |  6  |  5  |  4  |  3  |  2  |  1  |  0  |
    // |A1/T0| длина           |    код                |    
    //  атрибуты   0x80-вызовет ошибку
    // |  1  | длина атрибута  |    код атрибута       |
    // |  1  | 0 - пустой      |  1-15 0-недопустим    | 
    // |  1  | 1 - 1 байт      |  0-15                 | 
    // |  1  | 2 - 2 байт      |  0-15                 | 
    // |  1  | 3 - 4 байт      |  0-15                 | 
    // |  1  | 4 - 8 байт      |  0-15                 | 
    // |  1  | 5 - 16 байт     |  0-15                 | 
    // |  1  | 6 - 32 байта    |  0-15                 | 
    // |  1  | 7 - строка      |  0-15                 | 

    //  данные   0x00-вызовет ошибку
    // |  0  | длина данных    |    код типа           |

    // |  0  | 1 - 1 байт      |  0-15 четные с именем | 
    //|  нечетные без имени
    //|  (атрибут noname) 
    // |  0  | uint8_t         |  0-1    0x10 0x11     | 
    // |  0  | int8_t          |  2-3    0x12 0x13     | 
    // |  0  | char            |  4-5    0x14 0x15     | 

    // |  0  | 2 - 2 байт      |  0-15 четные с именем | 
    // |  0  | uint16_t        |  0-1    0x20 0x21     | 
    // |  0  | int16_t         |  2-3    0x22 0x23     | 

    // |  0  | 3 - 4 байт      |  0-15 четные с именем | 
    // |  0  | uint32_t        |  0-1    0x30-1        | 
    // |  0  | int32_t         |  2-3    0x32-3        | 
    // |  0  | float           |  4-5    0x34-5        | 

    // |  0  | 4 - 8 байт      |  0-15 четные с именем | 
    // |  0  | uint64_t        |  0  0x40-1            | 
    // |  0  | int64_t         |  2  0x42-3            | 
    // |  0  | double          |  4  0x44-5            | 
    // |  0  | 5 - 16 байт     |   
    // |  0  | 6 - 32 байт     |  
    // |  0  | 7 - 64 байт     |  строк нету, использовать char array

    //  структуры определение типа (typedef)  
    // |  0  | длина данных    |    код типа           |
    // |  0  |    0            |  1 без имени длина 1  | 1..FF
    // |  0  |    0            |  2 с  именем длина 1  | 
    // |  0  |    0            |  3 без имени длина 2  | 100..FFFF
    // |  0  |    0            |  4 с  именем длина 2  | 

    //   данные тип структура   
    // |  0  | длина данных    |    код типа           |
    // |  0  |    0            |  7 без имени          | 
    // |  0  |    0            |  8 с именем           | 
    // |  0  |    0            |  9 с именем из        |
    //                           определения структуры | 

    // порядок метаданных данных:
    // 0L,1H - длина метаданных 2 байта
    // массив определений структур которым присваиваетя порядковый номер IX от 0 до N вовремя парсинга:
    // 
    // тип структуры(1,2,3,4(,5,6))! длина! имя? массив_атрибутов?[atip!, value?]
    //     массив_данных![ dtip!, IX?(если dtip=7,8,9), имя?, массив_атрибутов?[atip!, value?] ]

    // последняя структура с атрибутами: export(видимо ненужен), adr, serial является корневой 
    //   как правило имеет данные типа структура с атрибутами WRK RAM EEP

    using Tip = Byte;
    using atrs_t = List<(Atr Atr, object? value)>;
    using datas_t = List<DataRoot>;

    internal static class TipExtention
    {
        internal static bool IsAttr(this Tip extention)
        {
            return extention > 0x80;
        }
        internal static bool IsData(this Tip extention)
        {
            return (extention <= 0x6F) && (extention >= 0x10);
        }
        /// <summary>
        /// Строки могут быть только атрибуты (ASCII 1251 нуль конец)
        /// </summary>
        /// <param name="extention"></param>
        /// <returns></returns>
        internal static bool IsString(this Tip extention)
        {
            return (extention & 0xF0) == 0xF0;
        }
        internal static bool IsStructDef(this Tip extention)
        {
            return (extention > 0) && (extention < 7);
        }
        internal static bool IsStructData(this Tip extention)
        {
            return (extention >= 7) && (extention <= 9);
        }
        internal static bool HasName(this Tip extention)
        {
            return !extention.IsAttr() && ((extention & 1) == 0);
        }
        internal static int Length(this Tip extention)
        {
            int lenpw = extention;

            if ((extention & 0x7F) == 0) return -1;
            else if (extention.IsString()) return -2;
            else if (extention.IsStructData()) return 1;
            else if (extention.IsStructDef())
            {
                if (!extention.HasName()) lenpw++;
                lenpw >>= 1;
            }
            else lenpw = (lenpw & 0x70) >> 4;

            return lenpw == 0 ? 0 : 1 << (lenpw - 1);
        }
    }

    #region value
    public unsafe class EmptyValue 
    {
        public virtual object? Value(byte* value, int ArrayLen = 1) => null;

        public static readonly EmptyValue Empty = new EmptyValue();

    }
    public unsafe abstract class UnmValue : EmptyValue 
    {
        public abstract object Default(int ArrayLen = 1);
        public abstract string ToString(object value, bool ShowHex = false, int ArrayLen = 1, int ArrayShowLen = 0);
        public abstract object StringToValue(string sval, bool ShowHex = false, int ArrayLen = 1);
    }
    public unsafe sealed class UnmValue<T>: UnmValue where T : unmanaged
    {
        public override object Default(int ArrayLen = 1)
        {
            if (ArrayLen == 1) return default(T);
            else
            {
                T[] res = new T[ArrayLen];
                return res;
            }
        }
        public override object? Value(byte* value, int ArrayLen = 1)
        {
            if (ArrayLen == 1) return *(T*)value;
            else
            {
                T[] res = new T[ArrayLen];
                for (int i= 0; i<res.Length;i++)
                {
                    res[i] = *(T*)value;
                    value += sizeof(T);
                }
                return res;
            }
        }
        public override string ToString(object value, bool ShowHex = false, int ArrayLen = 1, int ArrayShowLen = 0)
        {
            if (ArrayLen == 1)
            {
                T v = (T)value;
                if (ShowHex) return $"0x{v:X}";
                else return v.ToString()!;
            }
            else
            {
                T[] l = (T[])value;
                if (ArrayShowLen > 0 && (ArrayShowLen < l.Length))
                {
                    var s = new ArraySegment<T>(l, 0, ArrayShowLen);
                    if (ShowHex)
                    {
                        string[] sa = new string[s.Count];
                        for (int i = 0; i < s.Count; i++) sa[i] = $"0x{s[i]:X}";
                        return string.Join(" ", sa);
                    }
                    else return string.Join(" ", s);
                }
                if (ShowHex)
                {
                    string[] sa = new string[l.Length];
                    for (int i = 0; i < l.Length; i++) sa[i] = $"0x{l[i]:X}";
                    return string.Join(" ", sa);
                }
                else 
                {
                    if (typeof(T) == typeof(char))
                    {
                        for (int i = 0; i < l.Length; i++)
                        {
                            if (l[i].ToString() == "\0")
                            {
                                if (i == 0) return "";
                                var s = new ArraySegment<T>(l, 0, i);
                                return string.Join(" ", s);
                            }
                        }
                    }
                    return string.Join(" ", l);
                }                
            }
        }
        public override object StringToValue(string sval, bool ShowHex = false, int ArrayLen = 1)
        {
            if (ArrayLen ==1) return (T)Convert.ChangeType(ShowHex? Toint(sval) : sval, typeof(T));
            else
            {
                T[] v = new T[ArrayLen];
                var sa = sval.Split(' ');
                // sa.Length может быть меньше ArrayLen (ArrayShowLen атрибут)
                for (uint i = 0; i < sa.Length; i++) v[i] = (T)Convert.ChangeType(ShowHex ? Toint(sa[i]) : sa[i], typeof(T));
                return v;
            }
            static int Toint(string sval) => Convert.ToInt32(sval, 16);             
        }
    }
    public unsafe sealed class StrValue: EmptyValue
    {
        public static string SValue(byte* value, int ArrayLen = 1)
        {
            int l = 0;
            byte* p = value;
            while (*p != 0 && l < 128) { l++; p++; }
            return Encoding.GetEncoding(1251).GetString(value, l + 1).TrimEnd((Char)0);
        }
        public override object? Value(byte* value, int ArrayLen = 1) => SValue(value);        
    }
    #endregion

    public abstract class AttrConvertor
    {
        public unsafe abstract object Conv(byte* pData);
    }
    public class AttrConvertor<Tin, Tout> : AttrConvertor
        where Tin : unmanaged
        where Tout : unmanaged
    {
        public unsafe override object Conv(byte* pData)
        {
            Tin r = *(Tin*)pData;
            return Convert.ChangeType(r, typeof(Tout)); ;
        }
    }

    #region Data
    /// <summary>
    /// сущности содержащие данные    
    /// </summary>
    public abstract class DataRoot
    {
        public string name { get; internal set; } = string.Empty;
        public virtual int DataSize() => 0;

        public XmlSchema? GetSchema()
        {
            //XmlTextReader reader = new XmlTextReader("pack://application:,,,/MetaData/XMLSchemaMetaData.xsd");
            //XmlSchema? schema = XmlSchema.Read(reader, ValidationCallback);
            return null;
        }

        //private void ValidationCallback(object? sender, ValidationEventArgs e)
        //{
        //    if (e.Severity == XmlSeverityType.Warning)
        //        Console.Write("WARNING: ");
        //    else if (e.Severity == XmlSeverityType.Error)
        //        Console.Write("ERROR: ");

        //    Console.WriteLine(e.Message);
        //}
    }
    /// <summary>
    /// сущности имеющие конкретное место (смещение)
    /// </summary>
    public abstract class AnyVar : DataRoot
    {
        /// <summary>
        /// смешение в дочерней структуре
        /// </summary>                         
        public int offsetLocal { get; internal set; } = -1;
        /// <summary>
        /// смешение глобальное
        /// </summary>
        public int offsetGlobal { get; internal set; } = -1;
        internal virtual void updateOffsets(ref int glob, ref int loc)
        {
            int l = DataSize();
            offsetGlobal = glob;
            glob += l;
            offsetLocal = loc;
            loc += l;
        }
        protected void XSerAnyVarAttr(XmlWriter writer)
        {
            if (name != string.Empty) writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("global", offsetGlobal.ToString());
            writer.WriteAttributeString("local", offsetLocal.ToString());
        }
    }
    #endregion
    public enum StructTypes : Tip
    {
        // len = 1
        REC1_NONAM = 1,
        REC1_NAM = 2,
        // len = 2
        REC2_NONAM = 3,
        REC2_NAM = 4,
        // len = 4
        //REC4_NONAM = 5,
        //REC4_NAM = 6,
    }
    public enum StructInstance : Tip
    {
        REC_DAT_NONAM = 7, // без имени
        REC_DAT_NAM = 8,
        REC_DAT_SNAM = 9,  // без имени (но имя извлечъ из имени структуры)
    }
    public enum SimpleType: Tip
    {
        uint8_t_NAM     = 0x10,
        int8_t_NAM      = 0x12,
        char_NAM        = 0x14,

        uint16_t_NAM    = 0x20,
        int16_t_NAM     = 0x22,

        uint32_t_NAM    = 0x30,
        int32_t_NAM     = 0x32,
        float_NAM       = 0x34,

        uint64_t_NAM    = 0x40,
        int64_t_NAM     = 0x42,
        double_NAM      = 0x44,
    }
    public static class SimpleTypeDefs
    {
        public static readonly UnmValue StandartFloat = new UnmValue<float>();
        public static readonly UnmValue StandartByte = new UnmValue<byte>();
        public static readonly UnmValue StandartUshort = new UnmValue<ushort>();
        public static readonly UnmValue StandartUint = new UnmValue<uint>();
        public static readonly UnmValue StandartUlong = new UnmValue<ulong>();

        public static readonly (SimpleType t, UnmValue v)[] StandartTypeValue = new[]
        {
            (SimpleType.uint8_t_NAM, StandartByte),
            (SimpleType.int8_t_NAM, new UnmValue<sbyte>()),
            (SimpleType.char_NAM, new UnmValue<char>()),

            (SimpleType.uint16_t_NAM, StandartUshort),
            (SimpleType.int16_t_NAM, new UnmValue<short>()),

            (SimpleType.uint32_t_NAM, StandartUint),
            (SimpleType.int32_t_NAM, new UnmValue<int>()),
            (SimpleType.float_NAM, StandartFloat),

            (SimpleType.uint64_t_NAM, StandartUlong),
            (SimpleType.int64_t_NAM, new UnmValue<long>()),
            (SimpleType.double_NAM, new UnmValue<double>()),
        };

        public static readonly (int len, UnmValue v)[] UnkhownTypeValue = new[]
        {
            (1, StandartByte),
            (2, StandartUshort),
            (4, StandartUint),
            (8, StandartUlong),
        };
        public static SimpleType FromString(string elementName)
        {                    //рекомендуют для скорости явное приведение типа 
            foreach(SimpleType value in (SimpleType[]) Enum.GetValues(typeof(SimpleType)))
            {
                var s = value.ToString();
                s = s.Remove(s.LastIndexOf('_'));
                if (elementName == s) return value;
            }
            throw new ArgumentException($"FromString {elementName} not SimpleType");
        }
    }

    public enum Atr: Tip
    {
        // Len = 0
        WRK         =  0x81,
        RAM         =  0x82,
        EEP         =  0x83,
        export      = 0x84, // использовать adr, serial ?
        ReadOnly    = 0x85,
        ShowHex     = 0x86,
        // len = 1 
        adr                 = 0x90,
        chip                = 0x91,
        NoPowerDataCount    = 0x92,
        digits              = 0x93,
        precision           = 0x94,
        style               = 0x95,
        width               = 0x96,
        //// len=2
        serial              = 0xA0,
        RamSize             = 0xA1,
        SupportUartSpeed    = 0xA2,
        from                = 0xA3,
        adr_w               = 0xA4,
        //// len=4
        color               = 0xB0,
        SSDSize             = 0xB1,
        // array (int) avilable len 1,2
        array                = 0x9F,
        array_w              = 0xAF,
        // Show array (int) avilable len 1,2
        arrayShowLen       = 0x9E,
        arrayShowLen_w       = 0xAE,

        RangeLo_b = 0x9C,
        RangeLo_w = 0xAC,
        RangeLo = 0xBC,
        RangeHi_b = 0x9D,
        RangeHi_w = 0xAD,
        RangeHi = 0xBD,

        info    = 0xF0,
        metr    = 0xF1,
        eu      = 0xF2,
        title   = 0xF3,
        hint    = 0xF4,
    }
    public static class AttrDefs
    {
        public static readonly AttrConvertor<sbyte, float> SbyteToFloat = new AttrConvertor<sbyte, float>();
        public static readonly AttrConvertor<short, float> ShortToFloat = new AttrConvertor<short, float>();
        public static readonly AttrConvertor<ushort, int> UshortToInt = new AttrConvertor<ushort, int>();
        // атрибуты которые заменяются tst => Res с преобразованием типа
        public static readonly (Atr tst, Atr Res, AttrConvertor C)[] ConverterAttr = new[]
        {
            (Atr.adr_w, Atr.adr, UshortToInt as AttrConvertor),
            (Atr.array_w, Atr.array, UshortToInt),
            (Atr.arrayShowLen_w, Atr.arrayShowLen, UshortToInt),
            (Atr.RangeHi_b, Atr.RangeHi, SbyteToFloat),
            (Atr.RangeLo_b, Atr.RangeLo, SbyteToFloat),
            (Atr.RangeHi_w, Atr.RangeHi, ShortToFloat),
            (Atr.RangeLo_w, Atr.RangeLo, ShortToFloat),
        };

// атрибуты которые если объявлены в структуре то распространяютяа на все её данные
        public static readonly Atr[] RootAttr =
        {
           Atr.ReadOnly,
           Atr.ShowHex,
           Atr.digits,
           Atr.precision,
           Atr.style,
           Atr.width,
           Atr.color,
           Atr.arrayShowLen,
           Atr.RangeLo,
           Atr.RangeHi,
           Atr.eu,
           Atr.hint,
        };
        // все атрибуты преобразуются в int кроме len4 (color и SSDSize) в uint
        //атрибуты с нестандартными типами 
        public static readonly (Atr a, UnmValue v)[] UnStandartTypeAttr = new[]
        {
            (Atr.RangeLo, SimpleTypeDefs.StandartFloat),
            (Atr.RangeHi, SimpleTypeDefs.StandartFloat),
        };
        public static Atr? FromString(string attrName)
        {
            foreach (Atr value in (Atr[])Enum.GetValues(typeof(Atr)))
            {
                if (attrName == value.ToString()) return value;
            }
            return null;
        }
    }

    public class BinaryParser
    {
        public static readonly byte[] Meta_CAL = {
81,1,2,38,97,99,99,101,108,0,241,67,76,65,49,0,172,48,248,173,208,7,242,71,0,34,88,
0,34,89,0,34,90,0,172,96,240,173,160,15,1,64,158,30,35,175,167,2,34,100,49,0,175,
170,2,34,100,50,0,175,170,2,34,100,51,0,175,170,2,34,100,52,0,175,170,2,34,100,53,
0,175,170,2,34,100,54,0,175,170,2,34,100,55,0,175,170,2,34,100,56,0,175,170,2,2,38,
231,238,237,228,0,241,77,90,49,0,158,6,34,105,0,159,7,34,117,0,159,7,32,193,234,0,
188,154,153,153,190,189,102,102,70,64,2,19,195,202,0,241,71,75,49,0,242,236,234,208,
0,34,227,234,0,1,51,52,84,0,147,4,148,1,32,239,238,242,240,229,225,235,229,237,232,
229,0,9,3,9,0,9,2,240,228,224,237,237,251,229,32,231,238,237,228,238,226,0,159,3,
8,1,102,107,100,0,1,36,16,224,226,242,238,236,224,242,0,241,65,85,0,50,226,240,229,
236,255,0,241,87,84,0,8,4,67,97,108,105,112,101,114,0,1,23,50,226,240,229,236,255,
0,241,87,84,0,8,4,67,97,108,105,112,101,114,0,2,66,67,97,108,105,112,51,0,144,7,240,
50,50,46,48,50,46,50,48,50,52,32,49,52,58,50,50,58,52,49,32,207,240,238,244,232,235,
229,236,229,240,32,118,51,0,145,9,160,17,2,146,33,162,224,0,132,7,5,129,7,6,161,232,
253,130}; 

        public static readonly byte[] Meta_NNK = {
                212,1,1,14,242,109,86,0,32,236,231,0,32,225,231,0,1,28,52,197,236,234,238,241,242,
                252,95,225,224,242,224,240,229,232,0,176,255,0,0,127,242,65,104,0,2,175,99,104,97,
                114,103,101,0,16,115,116,97,116,117,115,0,133,52,200,231,240,224,241,245,238,228,
                238,226,224,237,237,224,255,95,229,236,234,238,241,242,252,0,176,255,0,0,127,242,
                65,104,0,147,10,148,7,52,206,241,242,224,226,248,224,255,241,255,95,229,236,234,238,
                241,242,252,0,176,255,0,0,127,242,65,104,0,133,147,5,148,2,16,208,229,241,243,240,
                241,0,133,176,255,0,0,127,242,37,0,52,210,229,236,239,229,240,224,242,243,240,224,
                0,176,0,255,31,127,242,67,0,133,147,5,148,1,52,205,224,239,240,255,230,229,237,232,
                229,95,225,224,242,224,240,229,232,0,133,242,86,0,147,5,148,2,52,210,238,234,0,133,
                242,109,65,0,147,5,148,1,1,16,50,226,240,229,236,255,0,241,87,84,0,133,9,2,1,18,8,
                0,205,205,202,0,8,1,193,224,242,224,240,229,255,0,1,35,8,4,205,224,241,242,240,238,
                233,234,224,0,163,0,0,8,3,209,225,240,238,241,95,231,224,240,255,228,224,0,163,0,
                2,2,22,205,205,202,0,241,78,78,75,49,50,56,0,32,236,231,0,32,225,231,0,2,23,97,99,
                99,101,108,0,241,67,76,65,49,0,34,88,0,34,89,0,34,90,0,1,32,16,224,226,242,238,236,
                224,242,0,241,65,85,0,50,226,240,229,236,255,0,241,87,84,0,9,2,9,7,9,6,1,19,50,226,
                240,229,236,255,0,241,87,84,0,9,2,9,7,9,6,2,84,78,110,107,49,50,56,0,144,5,240,48,
                50,46,48,50,46,50,48,50,52,32,57,58,52,56,58,53,48,32,32,78,78,75,49,50,56,32,66,
                79,79,84,69,69,80,32,67,104,97,114,103,101,114,32,65,99,99,101,108,0,145,6,160,1,
                0,162,250,0,146,9,132,7,8,129,7,9,161,0,1,130,7,5,131};

        public static readonly byte[] Meta_Ind = {
                169,3,1,17,32,98,97,100,95,98,108,111,99,107,115,0,134,159,40,1,168,20,109,97,110,
                117,102,97,99,116,117,114,101,114,0,176,255,0,0,0,159,13,20,109,111,100,101,108,0,
                176,0,255,0,0,159,21,32,100,97,116,97,95,98,121,116,101,115,95,112,101,114,95,112,
                97,103,101,0,134,32,115,112,97,114,101,95,98,121,116,101,115,95,112,101,114,95,112,
                97,103,101,0,134,16,112,97,103,101,115,95,112,101,114,95,98,108,111,99,107,0,134,
                32,98,108,111,99,107,115,95,112,101,114,95,108,117,110,0,134,16,109,97,120,95,98,
                97,100,95,98,108,111,99,107,115,95,112,101,114,95,108,117,110,0,134,16,103,117,97,
                114,101,110,116,101,101,100,95,118,97,108,105,100,95,98,108,111,99,107,115,0,134,
                1,123,48,115,105,103,110,97,116,117,114,101,0,176,0,0,255,0,48,115,101,114,105,97,
                108,0,176,0,255,31,0,32,76,49,0,159,8,32,76,50,0,159,8,32,70,0,159,8,34,65,105,114,
                95,122,122,0,159,8,32,241,228,226,232,227,95,236,229,230,228,243,95,234,226,224,228,
                240,224,242,243,240,224,236,232,0,159,4,48,68,95,115,111,110,100,101,95,109,109,0,
                34,65,105,114,95,122,122,95,97,109,116,0,159,8,32,115,101,114,118,105,99,101,0,159,
                70,2,34,242,229,234,243,249,232,233,95,240,229,233,241,0,32,240,229,233,241,0,32,
                225,235,238,234,95,239,224,236,255,242,232,0,1,61,32,240,229,233,241,0,32,247,232,
                241,235,238,95,225,235,238,234,238,226,0,32,225,235,238,234,95,239,224,236,255,242,
                232,95,237,224,247,224,235,238,0,32,225,235,238,234,95,239,224,236,255,242,232,95,
                234,238,237,229,246,0,1,54,16,82,101,115,101,116,82,101,115,101,114,118,0,176,0,0,
                255,0,8,3,242,229,234,243,249,232,233,95,240,229,233,241,0,8,4,232,241,242,238,240,
                232,255,95,240,229,233,241,238,226,0,159,3,1,132,176,255,0,0,0,16,224,234,241,229,
                235,229,240,238,236,229,242,240,0,32,236,238,228,243,235,252,95,226,232,234,0,16,
                239,224,236,255,242,252,95,228,224,237,237,251,229,95,237,229,95,231,224,239,232,
                241,224,237,251,0,16,239,224,236,255,242,252,95,231,224,239,240,238,241,95,237,224,
                95,247,242,229,237,232,229,95,241,95,239,229,240,229,239,238,235,237,229,237,232,
                229,236,0,16,239,224,236,255,242,252,95,244,238,240,236,224,242,232,240,238,226,224,
                237,232,229,0,16,239,224,236,255,242,252,0,1,26,8,6,238,248,232,225,234,232,0,8,4,
                242,229,234,243,249,232,233,95,240,229,233,241,0,1,62,8,1,78,65,78,68,0,163,0,0,8,
                0,66,97,100,66,108,111,99,107,115,0,163,0,2,8,2,109,101,116,114,73,110,100,0,163,
                0,4,8,7,114,97,109,68,97,116,97,0,163,0,8,8,5,101,101,112,0,163,0,16,2,112,86,73,
                75,0,48,115,105,103,110,97,116,117,114,101,0,48,99,111,110,100,105,116,105,111,110,
                0,52,82,111,95,115,109,116,0,159,8,52,80,72,95,115,109,116,0,159,8,48,102,114,97,
                109,101,0,52,116,101,109,112,101,114,97,116,117,114,101,0,52,65,77,95,82,88,95,49,
                0,159,10,52,65,77,95,82,88,95,50,0,159,10,52,80,72,95,82,88,95,49,0,159,10,52,80,
                72,95,82,88,95,50,0,159,10,2,20,97,99,99,101,108,0,34,88,0,34,89,0,34,90,0,18,84,
                0,1,36,16,224,226,242,238,236,224,242,0,241,65,85,0,50,226,240,229,236,255,0,241,
                87,84,0,8,10,97,99,99,101,108,0,9,9,1,23,50,226,240,229,236,255,0,241,87,84,0,8,10,
                97,99,99,101,108,0,9,9,2,67,73,110,100,0,144,6,240,50,54,46,48,49,46,50,48,50,52,
                32,49,48,58,49,56,58,49,54,32,32,73,78,68,32,69,69,80,32,66,79,79,84,69,69,80,0,145,
                6,160,1,0,162,250,0,132,7,11,129,7,12,161,0,1,130,7,8,131}; 


        //private static object AnyClone(object value)
        //{
        //    return value switch
        //    {
        //        sbyte  => (sbyte)   value,
        //        byte   => (byte)    value,  
        //        short  => (short)   value,
        //        ushort => (ushort)  value,
        //        int    => (int)     value,
        //        uint   => (uint)    value,
        //        long   => (long)    value,
        //        ulong  => (ulong)   value,
        //        float  => (float)   value,
        //        double => (double)  value,
        //        decimal=> (decimal) value,
        //        string => string.Copy((string)value),
        //              _=> value,
        //    } ;
        //}

        /// массив определений структур, последняя будет основной
        static List<StructDef> structDefs = new List<StructDef>();
        static BinaryParser()
        {
            // чтобы подключить 1251 
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        public static unsafe StructDef Parse(byte[] MetaData) 
        {
            // парсим из метаданных массив определений структур
            structDefs.Clear();
            fixed (byte* p = MetaData) 
            {
                ushort MetaSize = *(ushort*)p;
                byte* pMetaData = p;
                pMetaData += 2;
                ushort UsedLen = 2;
                while (UsedLen < MetaSize)
                {
                    var (sd, size) = StructDef.Factory(pMetaData);
                    UsedLen += size;
                    if (UsedLen > MetaSize) throw new Exception($"UsedLen {UsedLen} > MetaSize {MetaSize}");
                    pMetaData += size;
                    structDefs.Add(sd);
                }
            }
            var outs = structDefs.Last();
            // находим смещения данных в структурах данных RAM EEP WRK)
            foreach (var RootStruct in outs.datas) 
                if (RootStruct is StructVar s)
                {
                    int global = 0;
                    int local = 0;
                    s.updateOffsets(ref global, ref local);
                }
            return outs;
        }
        private static ((Atr Atr, object? value), int) GetArrayLen(atrs_t attr)
        {
            var a = attr.FirstOrDefault(a => a.Atr == Atr.array);// || a.Atr == Atr.array_w);
            return (a, (a == default) ? 1 : (int) a.value!);

        }
        private static void CloneDatas(datas_t src, datas_t dst)
        {
            foreach (var item in src)
            {
                if (item != null && item is ICloneable c) dst.Add((DataRoot)c.Clone());
            }
        }
        private static void CloneAttrs(atrs_t src, atrs_t dst)
        {
            //решено --TODO: наверное клонирование для атрибутов из метаданных ненужно они константы достаточно ссылок
            dst.AddRange(src);//---достаточно ссылок
            //foreach (var item in src)
            //{
            //    object? v = AnyClone(item.value);
            //    if (v != null && v == item.value) 
            //    {
            //        throw new Exception(); 
            //    }
            //    dst.Add((item.Atr, v));
            //}
        }
        private static unsafe string ParseString(ref byte* pMetaData, ref int UsedLen, int Alllen)
        {
            string s = StrValue.SValue(pMetaData);
            int l = s.Length+1;
            UsedLen += l;
            if (UsedLen > Alllen) throw new Exception($" In ParseString UsedLen{UsedLen} > Alllen {Alllen} ");
            pMetaData += l;
            return s;
        }
        private static unsafe void ParseAttr(ref byte* pMetaData, ref int UsedLen, int Alllen, atrs_t atrs)
        {
            while (UsedLen < Alllen) 
            {
                Tip t = *pMetaData;
                if (!t.IsAttr()) break;

                pMetaData++;
                UsedLen++;

                object? value = null;

                bool KnownAttr = Enum.IsDefined(typeof(Atr), t);

                int l = t.Length();

                if (l == -2)
                {
                    value = ParseString(ref pMetaData, ref UsedLen, Alllen);
                }
                else if (l > 0) 
                {
                    if (KnownAttr)
                    {   // проверка на нестандартный тип
                        var f = AttrDefs.UnStandartTypeAttr.FirstOrDefault(o => (Tip)o.a == t);
                        if (f != default)
                        {
                            // получение value нестандартного типа
                            value = f.v.Value(pMetaData);
                        }
                        else
                        {   // проверка на атрибут который нужно заменить и конвертировать тип
                            var ac = AttrDefs.ConverterAttr.FirstOrDefault(o=> (Tip)o.tst == t);
                            if (ac != default)
                            {
                                // замена атрибута
                                t = (Tip) ac.Res;
                                // получение value с заменой типа 
                                value = ac.C.Conv(pMetaData);
                            }
                            else
                                // стандартное получение value 
                                switch (l)
                              {
                                  case 1:
                                      value = (int) *pMetaData;
                                      break;
                                  case 2:
                                      value = (int) *(ushort*) pMetaData;
                                      break;
                                  case 4:
                                      value = *(uint*)pMetaData;
                                      break;
                                  default: throw new Exception("Error Attr Len");
                              }
                        }
                    }
                    pMetaData += l;
                    UsedLen += l;
                }
                if (KnownAttr)
                {
                    var a = atrs.FirstOrDefault(o => (Tip)o.Atr == t);
                    if (a == default) atrs.Add(((Atr)t, value));
                    // последний парсинг имеет преимущество (для структур)
                    else a.value = value;
                }
            }
        }
        private static unsafe void ParseData(ref byte* pMetaData, ref int UsedLen, int Alllen, datas_t datas)
        {
            while (UsedLen < Alllen)
            {
                if ((*pMetaData).IsData())
                {
                    var (d, l) = SimpleVar.Factory(pMetaData);
                    datas.Add(d);
                    pMetaData += l;
                    UsedLen += l;
                }
                else if ((*pMetaData).IsStructData())
                {
                    var (d, l) = StructVar.Factory(pMetaData);
                    // check array structures
                    var (a, aLen) = GetArrayLen(d.attrs);

                    if (aLen > 1)
                    {
                        d.attrs.Remove(a);
                        // expand array structure 1..aLen
                        for (int i = 1; i <= aLen; i++)
                        {
                            var ad = (StructVar)d.Clone();
                            ad.name = d.name + i;
                            datas.Add(ad);
                        }
                    }
                    else datas.Add(d);

                    pMetaData += l;
                    UsedLen += l;
                }
                else break;
            }
        }
        private static void XWriteAttrs(XmlWriter w, atrs_t attrs)
        {
            foreach (var attr in attrs)
            {
                if (attr.Atr == Atr.RAM || attr.Atr == Atr.EEP || attr.Atr == Atr.WRK)
                {
                    w.WriteAttributeString("RootPath", Enum.GetName(typeof(Atr), attr.Atr)!);
                }
                else
                {
                    w.WriteAttributeString(Enum.GetName(typeof(Atr), attr.Atr)!, attr.value == null ? null : attr.value.ToString());
                }                    
            }
        }
        private static void XReadAttrs(XmlReader reader, atrs_t attrs, DataRoot self)
        {
            if (reader.HasAttributes)
            {
                for (int i = 0; i < reader.AttributeCount; i++)
                {
                    reader.MoveToAttribute(i);
                    string an = reader.Name;
                    string av = reader.Value;
                    Atr? a = AttrDefs.FromString(an);
                    if (a != null)
                    {
                        Tip t = (Tip)a;
                        var f = AttrDefs.UnStandartTypeAttr.FirstOrDefault(o => (Tip)o.a == t);
                        if (f != default)
                        {
                            // получение value нестандартного типа
                            attrs.Add(((Atr)a, f.v.StringToValue(av)));
                        }
                        else switch (t.Length())
                            {
                                case -2:
                                    attrs.Add(((Atr)a, av));
                                    break;
                                case 0:
                                    attrs.Add(((Atr)a, null));
                                    break;
                                case 4:
                                    attrs.Add(((Atr)a, Convert.ToUInt32(av)));
                                    break;
                                default:
                                    attrs.Add(((Atr)a, Convert.ToInt32(av)));
                                    break;
                            }
                    }
                    else
                    {
                        if (an == "name") self.name = av;
                        else if (an == "RootPath")
                        {
                            attrs.Add(((Atr)AttrDefs.FromString(av)!, null));
                        }
                        else if (self is AnyVar anv)
                        {
                            if (an == "local") anv.offsetLocal = Convert.ToInt32(av);
                            else if (an =="global") anv.offsetGlobal = Convert.ToInt32(av);
                        }
                    };
                }
                reader.MoveToElement(); // Moves the reader back to the element node.
            }
        }
        private static void XWriteDatas(XmlWriter w, datas_t datas)
        {
            foreach (var d in datas ) if (d is IXmlSerializable xs)
            {
               if (d is SimpleVar sv) w.WriteStartElement(null, sv.typeText, StructDef.NAMESPACE);
               else w.WriteStartElement(null, "struct_t", StructDef.NAMESPACE);
               xs.WriteXml(w);
               w.WriteEndElement();
            }
        }
        private static void XReadDatas(XmlReader reader, datas_t datas)
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                return;
            }
            while (reader.Read() && reader.NodeType != XmlNodeType.EndElement) 
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName == "struct_t") datas.Add(StructVar.XFactory(reader));
                    else datas.Add(SimpleVar.XFactory(reader));
                }
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "struct_t")
                {

                }
            }                
        }

        /// <summary>
        /// unmanaged примитивные переменные
        /// </summary>
        public class SimpleVar : AnyVar, ICloneable, IXmlSerializable
        {
            #region Prroperty Tip 
            private Tip _tip;
            public Tip tip { get => _tip;
                private set
                {
                    if (_tip != value)
                    {
                        _tip = value;
                        byte Nametip = (byte)(tip & 0xFE);

                        simpleType = Enum.IsDefined(typeof(SimpleType), Nametip) ? (SimpleType)Nametip : null;

                        var s = Enum.GetName(typeof(SimpleType), Nametip);
                        typeText = (s == null) ? string.Empty : s.Remove(s.LastIndexOf('_'));

                        if (simpleType != null)
                        {
                            (_, unmValue) = SimpleTypeDefs.StandartTypeValue.FirstOrDefault(std => (byte)std.t == Nametip);
                        }
                        else
                        {
                            var v = SimpleTypeDefs.UnkhownTypeValue.FirstOrDefault(u => u.len == tip.Length());
                            if (v == default) throw new Exception("Unkhown Type Value !!!"); 
                            else unmValue = v.v;
                        }
                    }
                }
            }
            #endregion

            public SimpleType? simpleType { get; private set; }             
            /// <summary>
            /// имя XML элемента 
            /// </summary>
            public string typeText { get; private set; } = string.Empty;
            /// <summary>
            /// unmValue всегда имеет тип UnmValue<T>          
            /// </summary>
            public UnmValue unmValue { get; private set; } = SimpleTypeDefs.StandartByte;
            public object? LastValue { get; private set; } = null;

            #region Property ArrayLen
            private int _arrayLen = 0;
            public int ArrayLen
            {
                get
                {
                    if (_arrayLen == 0)
                    {
                        (_, _arrayLen) = GetArrayLen(attrs);
                    }
                    return _arrayLen;
                }
            }
            #endregion

            #region Property ShowHex
            private bool? _showhex;
            public bool ShowHex
            {
                get 
                {
                    if (_showhex == null)
                    {
                        var a = attrs.FirstOrDefault(a => a.Atr == Atr.ShowHex);
                        _showhex = a != default;
                    }
                    return (bool) _showhex;
                }
            }
            #endregion

            #region Property ArrayShowLen
            int _arrayShowLen = -1;
            public int ArrayShowLen 
            {
                get 
                {
                    if (_arrayShowLen == -1)
                    {
                        var a = attrs.FirstOrDefault(a => a.Atr == Atr.arrayShowLen);
                        if (a != default) _arrayShowLen = (int) a.value!;
                        else _arrayShowLen = 0;
                    }
                    return _arrayShowLen;
                }
            }

            #endregion

            public readonly atrs_t attrs = new();
            /// <summary>
            /// Основная функция: получение текущих данных 
            /// обновляется свойство LastValue
            /// </summary>
            /// <param name="data">указатель на массив данных</param>
            /// <returns>примитивные данные обернутые в объект</returns>
            public unsafe object? GetValue(byte* data)
            {
                byte* p = data + offsetGlobal;
                LastValue = unmValue.Value(p, ArrayLen);
                return LastValue;
            }
            public override int DataSize() => ArrayLen * tip.Length();
            private SimpleVar(Tip tip) 
            {
                this.tip = tip;
            }
            internal static SimpleVar XFactory(XmlReader reader)
            {
                SimpleType t = SimpleTypeDefs.FromString(reader.LocalName);
                var res = new SimpleVar((Tip)t);
                res.ReadXml(reader);
                return res;
            }
            internal static unsafe (SimpleVar, int) Factory(byte* pMetaData)
            {
                var res = new SimpleVar(*pMetaData++);
                int n = 1;
                if (res.tip.HasName()) res.name = ParseString(ref pMetaData, ref n, 129);
                ParseAttr(ref pMetaData, ref n, 0xFFFF, res.attrs);
                res.LastValue = res.unmValue.Default(res.ArrayLen);
                return (res, n);
            }
            public object Clone()
            {
                var res = new SimpleVar(tip);
                res.name = name;
                CloneAttrs(attrs, res.attrs);
                res.LastValue = LastValue;
                return res;
            }
            public void ReadXml(XmlReader reader)
            {
                XReadAttrs(reader,attrs,this);
                reader.Read(); // #Text or  EndElement/>
                if (reader.NodeType == XmlNodeType.Text)
                {
                    LastValue = reader.Value == string.Empty ? null : unmValue.StringToValue(reader.Value, ShowHex, ArrayLen);
                    reader.Read();
                }
                if (reader.NodeType == XmlNodeType.EndElement) return;
                throw new Exception("Bad XML SimpleVar");
            }
            public void WriteXml(XmlWriter writer)
            {
                XSerAnyVarAttr(writer);
                XWriteAttrs(writer, attrs);
                //try
                //{
                    if (LastValue != null) writer.WriteValue(unmValue.ToString(LastValue, ShowHex, ArrayLen));
                //}
                //catch
                //{
                //    throw;
                //};
            }
        }
        /// <summary>
        ///  переменная структура
        /// </summary>
        public class StructVar: AnyVar, ICloneable, IXmlSerializable
        {
            private readonly StructDef structVar;
            // берем все данные из определения структуры
            public atrs_t attrs => structVar.attrs;
            public datas_t datas => structVar.datas;
            public override int DataSize() => structVar.DataSize();
            internal override void updateOffsets(ref int glob, ref int loc)
            {
                if (attrs.FirstOrDefault(a => a.Atr == Atr.RAM || a.Atr == Atr.EEP || a.Atr == Atr.WRK || a.Atr == Atr.from) != default)
                {
                    glob = 0;
                    loc = 0;
                }
                offsetGlobal = glob;
                offsetLocal = loc;
                int l = 0;
                foreach (var data in structVar.datas) if (data is AnyVar av) av.updateOffsets(ref glob, ref l);
                loc += l;
            }
            private StructVar(StructDef structDef) 
            {
                // берем все данные из определения структуры
                this.structVar = (StructDef)structDef.Clone();
                // по умолчанию берем имя из структуры
                this.name = structDef.name; 
            }
            internal static StructVar XFactory(XmlReader reader)
            {
                var res = new StructVar(new StructDef());
                res.ReadXml(reader);
                return res;
            }
            internal static unsafe (StructVar, int) Factory(byte* pMetaData)
            {
                Tip t = *pMetaData++;
                byte ix = *pMetaData++;
                var res = new StructVar(structDefs[ix]);
                int n = 2;

                if (t.HasName()) res.name = ParseString(ref pMetaData, ref n, 129);
                //удалим имя даже если имя есть в определении структуры
                else if (t == (Tip)StructInstance.REC_DAT_NONAM) res.name = string.Empty;
                // добавляем атрибуты переменной к фактически к атрибутам определения структуры
                ParseAttr(ref pMetaData, ref n, 0xFFFF, res.attrs);                
                return (res, n);
            }
            public object Clone()
            {
                var res = new StructVar(structVar);
                res.name = name;
                return res;
            }
            public void ReadXml(XmlReader reader)
            {
                XReadAttrs(reader, attrs, this);
                XReadDatas(reader, datas);
            }
            public void WriteXml(XmlWriter writer)
            { 
                writer.WriteAttributeString("size", DataSize().ToString());
                XSerAnyVarAttr(writer);
                XWriteAttrs(writer, attrs);
                XWriteDatas(writer, datas);
            }
        }
        /// <summary>
        /// определение структуры 
        /// </summary>
        [XmlRoot("struct_t", Namespace = NAMESPACE)]
       // [XmlSchemaProvider("GetSchema")]
        public class StructDef : DataRoot, ICloneable, IXmlSerializable
        {
            public static readonly StructDef Empty = new StructDef();
            public const string NAMESPACE = "http://tempuri.org/horizont.pb";
            public const string NS_PX = "dvmtd";
            public const string SLTSTR = "D:\\Projects\\C#\\Communications\\MetaData\\structRoot.xsl";
            public const string SCHLOC = "D:\\Projects\\C#\\Communications\\MetaData\\XMLSchemaMetaData.xsd";

            public static XmlQualifiedName GetSchema(XmlSchemaSet xs)
            {
                XmlTextReader reader = new XmlTextReader(SCHLOC);
                XmlSchema? schema = XmlSchema.Read(reader, null);
                xs.Add(schema!);
                return new XmlQualifiedName("XMLMetaData", NAMESPACE);
            }

            public readonly atrs_t attrs = new ();
            public readonly datas_t datas = new ();
            public override int DataSize()
            {
                int res = 0;
                foreach (var data in datas) res += data.DataSize();
                return res;
            }
            internal static unsafe (StructDef, ushort ) Factory(byte* pMetaData)
            {
                Tip t = *pMetaData++;
                if (!Enum.IsDefined(typeof(StructTypes), t)) throw new Exception($"Bad StructTypes Tip {t} ");
                int n = t.Length();
                StructDef res = new StructDef();
                ushort size = n == 1 ? *pMetaData : *(ushort*)pMetaData;                
                pMetaData += n;
                n++;//tip

                if (t.HasName()) res.name = ParseString(ref pMetaData, ref n, size);

                ParseAttr(ref pMetaData, ref n, size, res.attrs);
                ParseData(ref pMetaData, ref n, size, res.datas);

                if (n != size) throw new Exception($"Bad Struct find size {n} != meta size {size} ");
                #region Expand root Arrts to Datas attrs
                for ( int i = 0; i < res.attrs.Count; i++)
                {
                    var attr = res.attrs[i];
                    if (AttrDefs.RootAttr.FirstOrDefault(a => a == attr.Atr) != default)
                    {                        
                        foreach (var data in res.datas) 
                        {
                            if (data is SimpleVar d)
                            {   // атрибуты самих данных имеют преимущество над root атрибутами 
                                if (d.attrs.FirstOrDefault(a => a.Atr == attr.Atr) == default)
                                {
                                    d.attrs.Add(attr);
                                }
                            }
                        }
                        // root атрибут больше ненужен 
                        res.attrs.Remove(attr); i--;
                    }
                }
                #endregion
                return (res, size);
            }
            public object Clone()
            {
                var res = new StructDef();
                res.name = name;
                CloneAttrs(attrs, res.attrs);
                CloneDatas(datas, res.datas);
                return res;
            }

            public void ReadXml(XmlReader reader)
            {
                reader.MoveToContent();
                if (reader.IsEmptyElement)
                {
                    return;
                }

                XReadAttrs(reader, attrs, this);
                XReadDatas(reader, datas);
                reader.ReadEndElement();
            }

            public void WriteXml(XmlWriter writer)
            {
                if (name != string.Empty) writer.WriteAttributeString("name", name);
                XWriteAttrs(writer, attrs);
                //writer.WriteAttributeString("xmlns", "xsi", null, XmlSchema.InstanceNamespace);
               // writer.WriteAttributeString("xsi", "schemaLocation", XmlSchema.InstanceNamespace, $"{NAMESPACE} {SCHLOC}");
                XWriteDatas(writer, datas);
            }
        }

    }
}
