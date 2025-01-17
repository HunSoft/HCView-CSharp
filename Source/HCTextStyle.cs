﻿/*******************************************************}
{                                                       }
{               HCView V1.1  作者：荆通                 }
{                                                       }
{      本代码遵循BSD协议，你可以加入QQ群 649023932      }
{            来获取更多的技术交流 2018-5-4              }
{                                                       }
{                 文本文字样式实现单元                  }
{                                                       }
{*******************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using HC.Win32;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;

namespace HC.View
{
    [Serializable]
    public enum HCFontStyle : byte
    {
        tsBold = 1, tsItalic = 2, tsUnderline = 4, tsStrikeOut = 8, tsSuperscript = 16, tsSubscript = 32
    }

    public class HCTextStyle : HCObject
    {
        public const Single DefaultFontSize = 10.5F;  // 五号
        public const string DefaultFontFamily = "宋体";
        public const Single MaxFontSize = 512F;

        private Single FSize;
        private int FFontHeight;
        private string FFamily;
        private HCFontStyles FFontStyles;
        private Color FColor;  // 字体颜色
        private Color FBackColor;
        private TEXTMETRICW FTextMetric;

        bool FCJKFont;
        uint FOutMetSize;
        IntPtr FOutlineTextmetricPtr;
        OUTLINETEXTMETRICW FOutlineTextmetric;
        TT_HHEA FFontHeader;

        protected void SetFamily(string value)
        {
            if (FFamily != value)
                FFamily = value;
        }

        protected void SetSize(Single value)
        {
            if (FSize != value)
                FSize = value;
        }

        protected void SetFontStyles(HCFontStyles value)
        {
            if (FFontStyles != value)
                FFontStyles = value;
        }

        public bool CheckSaveUsed;
        public int TempNo;

        public HCTextStyle()
        {
            FSize = DefaultFontSize;
            FFamily = DefaultFontFamily;
            FFontStyles = new HCFontStyles();
            FColor = Color.Black;
            FBackColor = HC.HCTransparentColor;
            FTextMetric = new TEXTMETRICW();
            FOutMetSize = 0;

            FOutlineTextmetric = new OUTLINETEXTMETRICW();
            FFontHeader = new TT_HHEA();
        }

        ~HCTextStyle()
        {
            if (FOutMetSize > 0)
                Marshal.FreeHGlobal(FOutlineTextmetricPtr);
        }

        private const int MS_HHEA_TAG = 0x61656868;  // MS_MAKE_TAG('h','h','e','a')
        private const uint CJK_CODEPAGE_BITS = (1 << 17) | (1 << 18) | (1 << 19) | (1 << 20) | (1 << 21);
        public void ApplyStyle(HCCanvas aCanvas, Single aScale = 1)
        {
            if (FBackColor == HC.HCTransparentColor)
                aCanvas.Brush.Style = HCBrushStyle.bsClear;
            else
                aCanvas.Brush.Color = FBackColor;

            aCanvas.Font.BeginUpdate();
            try
            {
                aCanvas.Font.Color = FColor;
                aCanvas.Font.Family = FFamily;
                if ((FFontStyles.Contains((byte)HCFontStyle.tsSuperscript)) || (FFontStyles.Contains((byte)HCFontStyle.tsSubscript)))
                    aCanvas.Font.Size = FSize / 2;
                else
                    aCanvas.Font.Size = FSize;

                aCanvas.Font.FontStyles = FFontStyles;
            }
            finally
            {
                aCanvas.Font.EndUpdate();
            }

            aCanvas.GetTextMetrics(ref FTextMetric);

            FONTSIGNATURE vFontSignature = new FONTSIGNATURE();
            FFontHeight = aCanvas.TextHeight("H");

            if ((GDI.GetTextCharsetInfo(aCanvas.Handle, ref vFontSignature, 0) != GDI.DEFAULT_CHARSET)
                && ((vFontSignature.fsCsb[0] & CJK_CODEPAGE_BITS) != 0))
                FCJKFont = true;
            else
                FCJKFont = false;

            if (FOutMetSize > 0)
                Marshal.FreeHGlobal(FOutlineTextmetricPtr);


            FOutMetSize = GDI.GetOutlineTextMetrics(aCanvas.Handle, 0, IntPtr.Zero);
            if (FOutMetSize != 0)
            {
                //FOutMetSize = (uint)Marshal.SizeOf(FOutlineTextmetric);
                FOutlineTextmetricPtr = Marshal.AllocHGlobal((int)FOutMetSize);


                if (GDI.GetOutlineTextMetrics(aCanvas.Handle, FOutMetSize, FOutlineTextmetricPtr) != 0)
                {
                    //FOutlineTextmetric = new OUTLINETEXTMETRICW();
                    //FOutlineTextmetric = (OUTLINETEXTMETRICW)Marshal.PtrToStructure(FOutlineTextmetricPtr, typeof(OUTLINETEXTMETRICW));
                    //string otmpFamilyName = Marshal.PtrToStringUni(new IntPtr((int)FOutlineTextmetricPtr + (int)FOutlineTextmetric.otmpFamilyName));
                    //string otmpFaceName = Marshal.PtrToStringUni(new IntPtr((int)FOutlineTextmetricPtr + (int)FOutlineTextmetric.otmpFaceName)); ;
                    //string otmpStyleName = Marshal.PtrToStringUni(new IntPtr((int)FOutlineTextmetricPtr + (int)FOutlineTextmetric.otmpStyleName)); ;
                    //string otmpFullName = Marshal.PtrToStringUni(new IntPtr((int)FOutlineTextmetricPtr + (int)FOutlineTextmetric.otmpFullName)); ;
                    // 以上为参考代码
                    //Marshal.PtrToStructure(FOutlineTextmetricPtr, FOutlineTextmetric);
                    FOutlineTextmetric = (OUTLINETEXTMETRICW)Marshal.PtrToStructure(FOutlineTextmetricPtr, typeof(OUTLINETEXTMETRICW));
                }                
            }

            if ((uint)GDI.GetFontData(aCanvas.Handle, MS_HHEA_TAG, 0, ref FFontHeader, Marshal.SizeOf(FFontHeader)) == GDI.GDI_ERROR)
                return;
        }

        public bool EqualsEx(HCTextStyle aSource)
        {
            return (this.FSize == aSource.Size)
                && (this.FFontStyles.Value == aSource.FontStyles.Value)
                && (this.FFamily == aSource.Family)
                && (this.FColor == aSource.Color)
                && (this.FBackColor == aSource.BackColor);
        }

        public void AssignEx(HCTextStyle aSource)
        {
            this.FSize = aSource.Size;
            this.FFontStyles.Value = aSource.FontStyles.Value;
            this.FFamily = aSource.Family;
            this.FColor = aSource.Color;
            this.FBackColor = aSource.BackColor;
        }

        public void SaveToStream(Stream aStream)
        {
            byte[] vBuffer = BitConverter.GetBytes(FSize);
            aStream.Write(vBuffer, 0, vBuffer.Length);

            HC.HCSaveTextToStream(aStream, FFamily);

            aStream.WriteByte(FFontStyles.Value);  // save FFontStyles

            HC.HCSaveColorToStream(aStream, FColor);  // save FColor
            HC.HCSaveColorToStream(aStream, FBackColor);  // save FBackColor
        }

        public void LoadFromStream(Stream aStream, ushort aFileVersion)
        {
            int vOldSize = 10;
            if (aFileVersion < 12)
            {
                byte[] vBuffer1 = BitConverter.GetBytes(vOldSize);
                aStream.Read(vBuffer1, 0, vBuffer1.Length);
                vOldSize = BitConverter.ToInt32(vBuffer1, 0);
                FSize = (ushort)vOldSize;
            }
            else
            {
                byte[] vBuffer1 = BitConverter.GetBytes(FSize);
                aStream.Read(vBuffer1, 0, vBuffer1.Length);
                FSize = BitConverter.ToSingle(vBuffer1, 0);  // 字号
            }

            HC.HCLoadTextFromStream(aStream, ref FFamily, aFileVersion); // 字体

            FFontStyles.Value = (byte)aStream.ReadByte();  // load FFontStyles

            HC.HCLoadColorFromStream(aStream, ref FColor);  // load FColor
            HC.HCLoadColorFromStream(aStream, ref FBackColor);  // load FBackColor
        }

        // ToCSS 子方法
        private string GetTextDecoration()
        {
            string Result = "";
            if (FFontStyles.Contains((byte)HCFontStyle.tsUnderline))
                Result = " underline";

            if (FFontStyles.Contains((byte)HCFontStyle.tsStrikeOut))
            {
                if (Result != "")
                    Result = Result + ", line-through";
                else
                    Result = " line-through";
            }

            return "text-decoration:" + Result + ";";
        }

        public string ToCSS()
        {
            string Result = string.Format(" font-size: #.#pt", FSize)
                + string.Format(" font-family: {0};", FFamily)
                + string.Format(" color:rgb({0}, {1}, {2});", FColor.R, FColor.G, FColor.B)
                + string.Format(" background-color:rgb({0}, {1}, {2});", FBackColor.R, FBackColor.G, FBackColor.B);

            if (FFontStyles.Contains((byte)HCFontStyle.tsItalic))
                Result = Result + string.Format(" font-style: {0};", "italic");
            else
                Result = Result + string.Format(" font-style: {0};", "normal");

            if (FFontStyles.Contains((byte)HCFontStyle.tsBold) || FFontStyles.Contains((byte)HCFontStyle.tsStrikeOut))
                Result = Result + string.Format(" font-weight: {0};", "bold");
            else
                Result = Result + string.Format(" font-weight: {0};", "normal");

            if (FFontStyles.Contains((byte)HCFontStyle.tsUnderline) || FFontStyles.Contains((byte)HCFontStyle.tsStrikeOut))
                Result = Result + " " + GetTextDecoration();

            if (FFontStyles.Contains((byte)HCFontStyle.tsSuperscript))
                Result = Result + " " + " vertical-align:super;";

            if (FFontStyles.Contains((byte)HCFontStyle.tsSubscript))
                Result = Result + " " + " vertical-align:sub;";

            return Result;
        }

        // ToXml子方法
        private string GetFontStyleXML()
        {
            string Result = "";
            if (FFontStyles.Contains((byte)HCFontStyle.tsBold))
                Result = "bold";

            if (FFontStyles.Contains((byte)HCFontStyle.tsItalic))
            {
                if (Result != "")
                    Result = Result + ", italic";
                else
                    Result = "italic";
            }

            if (FFontStyles.Contains((byte)HCFontStyle.tsUnderline))
            {
                if (Result != "")
                    Result = Result + ", underline";
                else
                    Result = "underline";
            }

            if (FFontStyles.Contains((byte)HCFontStyle.tsStrikeOut))
            {
                if (Result != "")
                    Result = Result + ", strikeout";
                else
                    Result = "strikeout";
            }

            if (FFontStyles.Contains((byte)HCFontStyle.tsSuperscript))
            {
                if (Result != "")
                    Result = Result + ", sup";
                else
                    Result = "sup";
            }

            if (FFontStyles.Contains((byte)HCFontStyle.tsSubscript))
            {
                if (Result != "")
                    Result = Result + ", sub";
                else
                    Result = "sub";
            }

            return Result;
        }

        public void ToXml(XmlElement aNode)
        {
            aNode.Attributes["size"].Value = string.Format("{0:0.#}", FSize);
            aNode.Attributes["color"].Value = HC.GetColorXmlRGB(FColor);
            aNode.Attributes["bkcolor"].Value = HC.GetColorXmlRGB(FBackColor);
            aNode.Attributes["style"].Value = GetFontStyleXML();
            aNode.InnerText = FFamily;
        }

        public void ParseXml(XmlElement aNode)
        {
            FFamily = aNode.InnerText;
            FSize = float.Parse(aNode.Attributes["size"].Value);
            FColor = HC.GetXmlRGBColor(aNode.Attributes["color"].Value);
            FBackColor = HC.GetXmlRGBColor(aNode.Attributes["bkcolor"].Value);

            string[] vsStyles = aNode.Attributes["style"].Value.Split(new string[] { "," }, StringSplitOptions.None);
            for (int i = 0; i < vsStyles.Length; i++)
            {
                if (vsStyles[i] == "bold")
                    FFontStyles.InClude((byte)HCFontStyle.tsBold);
                else
                if (vsStyles[i] == "italic")
                    FFontStyles.InClude((byte)HCFontStyle.tsItalic);
                else
                if (vsStyles[i] == "underline")
                    FFontStyles.InClude((byte)HCFontStyle.tsUnderline);
                else
                if (vsStyles[i] == "strikeout")
                    FFontStyles.InClude((byte)HCFontStyle.tsStrikeOut);
                else
                if (vsStyles[i] == "sup")
                    FFontStyles.InClude((byte)HCFontStyle.tsSuperscript);
                else
                if (vsStyles[i] == "sub")
                    FFontStyles.InClude((byte)HCFontStyle.tsSubscript);
            }
        }

        public TEXTMETRICW TextMetric
        {
            get { return FTextMetric; }
        }

        public uint OutMetSize
        {
            get { return FOutMetSize; }
        }

        public OUTLINETEXTMETRICW OutlineTextmetric
        {
            get { return FOutlineTextmetric; }
        }

        public TT_HHEA FontHeader
        {
            get { return FFontHeader; }
        }

        public bool CJKFont
        {
            get { return FCJKFont; }
        }

        public string Family
        {
            get { return FFamily; }
            set { SetFamily(value); }
        }

        public Single Size
        {
            get { return FSize; }
            set { SetSize(value); }
        }

        public int FontHeight
        {
            get { return FFontHeight; }
        }

        public HCFontStyles FontStyles
        {
            get { return FFontStyles; }
            set { SetFontStyles(value); }
        }

        public Color Color
        {
            get { return FColor; }
            set { FColor = value; }
        }

        public Color BackColor
        {
            get { return FBackColor; }
            set { FBackColor = value; }
        }
    }
}
