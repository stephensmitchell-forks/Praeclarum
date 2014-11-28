using System;
using Praeclarum.Graphics;
using System.Runtime.InteropServices;

#if MONOTOUCH
using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;
using NativeNSMutableAttributedString = MonoTouch.Foundation.NSMutableAttributedString;
using NativeCTStringAttributes = MonoTouch.CoreText.CTStringAttributes;
using NativeColor = MonoTouch.UIKit.UIColor;
using CGColor = MonoTouch.CoreGraphics.CGColor;
#elif MONOMAC
using MonoMac.CoreText;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;
using NativeNSMutableAttributedString = MonoMac.Foundation.NSMutableAttributedString;
using NativeCTStringAttributes = MonoMac.CoreText.CTStringAttributes;
using NativeColor = MonoMac.AppKit.NSColor;
using CGColor = MonoMac.CoreGraphics.CGColor;
#endif

namespace Praeclarum
{
	public class CTStringAttributesWrapper : IRichTextAttributes
	{
		NativeCTStringAttributes attrs;

		public NativeCTStringAttributes Attributes { get { return attrs; } }

		public CTStringAttributesWrapper (string className)
		{
			attrs = new NativeCTStringAttributes ();
			ClassName = className;
		}

		#if MONOTOUCH
		static readonly NSString FontAttributeName = new NSString ("NSFont");
		static readonly NSString ForegroundColorAttributeName = new NSString ("NSColor");
		static readonly NSString BackgroundColorAttributeName = new NSString ("NSBackgroundColor");
		static readonly NSString UnderlineColorAttributeName = new NSString ("NSUnderlineColor");
		public static readonly NSString LinkAttributeName = new NSString ("NSLink");
		#elif MONOMAC
		public static readonly NSString ForegroundColorAttributeName = NSAttributedString.ForegroundColorAttributeName;
		public static readonly NSString BackgroundColorAttributeName = NSAttributedString.BackgroundColorAttributeName;
		public static readonly NSString LinkAttributeName = NSAttributedString.LinkAttributeName;
		#endif


		#region IRichTextAttributes implementation

		public string ClassName { get; set; }

		string fontName = "";
		public string FontName {
			get {
				return fontName;
			}
			set {
				fontName = value;
				SetFont ();
			}
		}

		float size = 0;
		public float FontSize {
			get {
				return size;
			}
			set {
				size = value;
				SetFont ();
			}
		}

		void SetFont ()
		{
			if (string.IsNullOrEmpty (fontName) || size <= 0.1f)
				return;

			#if MONOTOUCH
			attrs.Dictionary.SetValue (FontAttributeName, MonoTouch.UIKit.UIFont.FromName (fontName, size));
			#elif MONOMAC
			attrs.Font = new CTFont (fontName, size);
			#endif
		}

		public bool UseCGColor = false;

		INativeObject ToNativeColor (Color color)
		{
			if (UseCGColor) {
				return new CGColor (
					color.RedValue,
					color.GreenValue,
					color.BlueValue,
					color.AlphaValue);
			}
			#if MONOTOUCH
			return NativeColor.FromRGBA (
				color.Red,
				color.Green,
				color.Blue,
				color.Alpha);
			#elif MONOMAC
			return NativeColor.FromDeviceRgba (
				color.RedValue,
				color.GreenValue,
				color.BlueValue,
				color.AlphaValue);
			#endif
		}

		Color foregroundColor = Colors.Empty;
		public Color ForegroundColor {
			get {
				return foregroundColor;
			}
			set {
				foregroundColor = value;
				attrs.Dictionary.SetValue (ForegroundColorAttributeName, ToNativeColor (value));
			}
		}

		Color backgroundColor = Colors.Empty;
		public Color BackgroundColor {
			get {
				return backgroundColor;
			}
			set {
				backgroundColor = value;
				attrs.Dictionary.SetValue (BackgroundColorAttributeName, ToNativeColor (backgroundColor));
			}
		}

		UnderlineStyle underlineStyle;
		public UnderlineStyle UnderlineStyle {
			get {
				return underlineStyle;
			}
			set {
				underlineStyle = value;
				SetUnderline ();
			}
		}

		Color underlineColor = Colors.Empty;
		public Color UnderlineColor {
			get {
				return underlineColor;
			}
			set {
				underlineColor = value;
				SetUnderline ();
			}
		}

		void SetUnderline ()
		{
			#if MONOTOUCH
			var color = MonoTouch.UIKit.UIColor.FromRGBA (
				underlineColor.Red,
				underlineColor.Green,
				underlineColor.Blue,
				underlineColor.Alpha);
			attrs.UnderlineColor = color.CGColor;
			attrs.Dictionary.SetValue (UnderlineColorAttributeName, color);
			attrs.UnderlineStyle = underlineStyle == UnderlineStyle.None ? 
				MonoTouch.CoreText.CTUnderlineStyle.None : 
			                       MonoTouch.CoreText.CTUnderlineStyle.Single;
			#elif MONOMAC
			attrs.Dictionary.SetValue (NSAttributedString.UnderlineColorAttributeName, ToNativeColor (underlineColor));
			var ns = underlineStyle == UnderlineStyle.None ? 0 :
			         (underlineStyle == UnderlineStyle.Single ? 0x1 : 0x9);
			attrs.Dictionary.SetValue (NSAttributedString.UnderlineStyleAttributeName, NSNumber.FromInt32 (ns));
			#endif
		}

		string link = "";
		public string Link {
			get {
				return link;
			}
			set {
				link = value;
				if (!string.IsNullOrEmpty (link)) {
					#if MONOTOUCH
					attrs.Dictionary.SetValue (LinkAttributeName, new NSString (link));
					#elif MONOMAC
					attrs.Dictionary.SetValue (LinkAttributeName, new NSString (link));
					#endif
				}
			}
		}

		void SetLink ()
		{
		}

		#endregion
	}

	public class NSMutableAttributedStringWrapper : IRichText
	{
		NativeNSMutableAttributedString s;

		public NSMutableAttributedStringWrapper (NativeNSMutableAttributedString ns)
		{
			s = ns;
		}

		public NSMutableAttributedStringWrapper (string data)
		{
			s = new NativeNSMutableAttributedString (data);
		}

		public NativeNSMutableAttributedString AttributedText {
			get { return s; }
		}

		#region NSMutableAttributedString implementation

		public void AddAttributes (IRichTextAttributes styleClass, StringRange range)
		{
			var attrs = ((CTStringAttributesWrapper)styleClass).Attributes;
			s.AddAttributes (attrs, new NSRange (range.Location, range.Length));
		}

		#endregion
	}

	public static class StringRangeEx
	{
		public static StringRange ToStringRange (this NSRange range)
		{
			return new StringRange (range.Location, range.Length);
		}

		public static NSRange ToNSRange (this StringRange r)
		{
			return new NSRange (r.Location, r.Length);
		}
	}

	public static class NSDictionaryEx
	{
		[DllImport ("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
		private static extern void CFDictionarySetValue (IntPtr theDict, IntPtr key, IntPtr value);

		public static void SetValue (this NSDictionary theDict, NSString key, INativeObject value)
		{
			SetValue (theDict.Handle, key.Handle, value.Handle);
		}

		static void SetValue (IntPtr theDict, IntPtr key, IntPtr value)
		{
			CFDictionarySetValue (theDict, key, value);
		}

		public static void AddAttributes (this NativeNSMutableAttributedString s, NativeCTStringAttributes a, StringRange r)
		{
			s.AddAttributes (a, new NSRange (r.Location, r.Length));
		}
	}
}

