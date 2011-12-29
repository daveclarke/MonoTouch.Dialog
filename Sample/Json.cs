using System;
using System.IO;
using System.Json;
using System.Reflection;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace MonoTouch.Dialog {
	public class JsonDialog {
		public static RootElement FromFile (string file, object arg)
		{
			using (var reader = File.OpenRead (file))
				return FromJson (JsonObject.Load (reader) as JsonObject, arg);
		}
		
		public static RootElement FromFile (string file)
		{
			return FromFile (file, null);
		}
		
		static string GetString (JsonValue obj, string key)
		{
			if (obj.ContainsKey (key))
				if (obj [key].JsonType == JsonType.String)
					return (string) obj [key];
			return null;
		}

		static JsonArray GetArray (JsonObject obj, string key)
		{
			if (obj.ContainsKey (key))
				if (obj [key].JsonType == JsonType.Array)
					return (JsonArray) obj [key];
			return null;
		}
		
		static bool GetBoolean (JsonObject obj, string key)
		{
			try {
				return (bool) obj [key];
			} catch {
				return false;
			}
		}
		
		public static RootElement FromJson (JsonObject json)
		{
			return FromJson (json);
		}
				
		public static RootElement FromJson (JsonObject json, object data)
		{
			if (json == null)
				return null;

			var root = new RootElement (GetString (json, "title") ?? "");
			LoadSections (root, GetArray (json, "sections"), data);
			return root;
		}
		
		static void Error (string msg)
		{
			Console.WriteLine (msg);
		}
		
		static void Error (string fmt, params object [] args)
		{
			Error (String.Format (fmt, args));
		}
		
		static void LoadSections (RootElement target, JsonArray array, object data)
		{
			if (array == null)
				return;
			int n = array.Count;
			for (int i = 0; i < n; i++){
				var jsonSection = array [i];
				var header = GetString (jsonSection, "header");
				var footer = GetString (jsonSection, "footer");
				
				var section = new Section (header, footer);
				if (jsonSection.ContainsKey ("elements"))
					LoadSectionElements (section, jsonSection ["elements"] as JsonArray, data);
				target.Add (section);
			}
		}
		
		static string bundlePath;
		
		static string ExpandPath (string path)
		{
			if (path != null && path.Length > 1 && path [0] == '~' && path [1] == '/'){
				if (bundlePath == null)
					bundlePath = NSBundle.MainBundle.BundlePath;
					
				return Path.Combine (bundlePath, path.Substring (2));
			}
			return path;
		}
		
		static Element LoadBoolean (JsonObject json)
		{
			var caption = GetString (json, "caption");
			bool bvalue = GetBoolean (json, "value");
			var onImagePath = ExpandPath (GetString (json, "on"));
			var offImagePath = ExpandPath (GetString (json, "off"));

			if (onImagePath != null && offImagePath != null){
				var onImage = UIImage.FromFile (onImagePath);
				var offImage = UIImage.FromFile (offImagePath);
				
				return new BooleanImageElement (caption, bvalue, onImage, offImage);
			} else 
				return new BooleanElement (caption, bvalue);
		}
		
		static UIKeyboardType ToKeyboardType (string kbdType)
		{
			switch (kbdType){
			case "numbers": return UIKeyboardType.NumberPad;
			case "default": return UIKeyboardType.Default;
			case "ascii": return UIKeyboardType.ASCIICapable;
			case "numbers-and-punctuation": return UIKeyboardType.NumbersAndPunctuation;
			case "decimal": return UIKeyboardType.DecimalPad;
			case "email": return UIKeyboardType.EmailAddress;
			case "name": return UIKeyboardType.NamePhonePad;
			case "twitter": return UIKeyboardType.Twitter;
			case "url": return UIKeyboardType.Url;
			default:
				Console.WriteLine ("Unknown keyboard type: {0}, valid values are numbers, default, ascii, numbers-and-punctuation, decimal, email, name, twitter and url", kbdType);
				break;
			}
			return UIKeyboardType.Default;
		}
		
		static UIReturnKeyType ToReturnKeyType (string returnKeyType)
		{
			switch (returnKeyType){
			case "default": return UIReturnKeyType.Default;
			case "done": return UIReturnKeyType.Done;
			case "emergencycall": return UIReturnKeyType.EmergencyCall;
			case "go": return UIReturnKeyType.Go;
			case "google": return UIReturnKeyType.Google;
			case "join": return UIReturnKeyType.Join;
			case "next": return UIReturnKeyType.Next;
			case "route": return UIReturnKeyType.Route;
			case "search": return UIReturnKeyType.Search;
			case "send": return UIReturnKeyType.Send;
			case "yahoo": return UIReturnKeyType.Yahoo;
			default:
				Console.WriteLine ("Unknown return key type `{0}', valid values are default, done, emergencycall, go, google, join, next, route, search, send and yahoo");
				break;
			}
			return UIReturnKeyType.Default;
		}
		
		static UITextAutocapitalizationType ToAutocapitalization (string auto)
		{
			switch (auto){
			case "sentences": return UITextAutocapitalizationType.Sentences;
			case "none": return UITextAutocapitalizationType.None;
			case "words": return UITextAutocapitalizationType.Words;
			case "all": return UITextAutocapitalizationType.AllCharacters;
			default:
				Console.WriteLine ("Unknown autocapitalization value: `{0}', allowed values are sentences, none, words and all");
				break;
			}
			return UITextAutocapitalizationType.Sentences;
		}
		
		static UITextAutocorrectionType ToAutocorrect (JsonValue value)
		{
			if (value.JsonType == JsonType.Boolean)
				return ((bool) value) ? UITextAutocorrectionType.Yes : UITextAutocorrectionType.No;
			if (value.JsonType == JsonType.String){
				var s = ((string) value);
				if (s == "yes") 
					return UITextAutocorrectionType.Yes;
				return UITextAutocorrectionType.No;
			}
			return UITextAutocorrectionType.Default;
		}
		
		static Element LoadEntry (JsonObject json, bool isPassword)
		{
			var caption = GetString (json, "caption");
			var value = GetString (json, "value");
			var placeholder = GetString (json, "placeholder");
			
			var element = new EntryElement (caption, placeholder, value, isPassword);
			
			if (json.ContainsKey ("keyboard"))
				element.KeyboardType = ToKeyboardType (GetString (json, "keyboard"));
			if (json.ContainsKey ("return-key"))
				element.ReturnKeyType = ToReturnKeyType (GetString (json, "return-key"));
			if (json.ContainsKey ("capitalization"))
				element.AutocapitalizationType = ToAutocapitalization (GetString (json, "capitalization"));
			if (json.ContainsKey ("autocorrect"))
				element.AutocorrectionType = ToAutocorrect (json ["autocorrect"]);
			
			return element;
		}
		
		static UITableViewCellAccessory ToAccessory (string accesory)
		{
			switch (accesory){
			case "checkmark": return UITableViewCellAccessory.Checkmark;
			case "detail-disclosure": return UITableViewCellAccessory.DetailDisclosureButton;
			case "disclosure-indicator": return UITableViewCellAccessory.DisclosureIndicator;
			}
			return UITableViewCellAccessory.None;
		}
		
		static int FromHex (char c)
		{
			if (c >= '0' && c <= '9')
				return c-'0';
			if (c >= 'a' && c <= 'f')
				return c-'a'+10;
			if (c >= 'A' && c <= 'F')
				return c-'A'+10;
			Console.WriteLine ("Unexpected `{0}' in hex value for color", c);
			return 0;
		}
		
		static void ColorError (string text)
		{
			Console.WriteLine ("Unknown color specification {0}, expecting #rgb, #rgba, #rrggbb or #rrggbbaa formats", text);
		}
		
		static UIColor ParseColor (string text)
		{
			int tl = text.Length;
			
			if (tl > 1 && text [0] == '#'){
				int r, g, b, a;
				
				if (tl == 4 || tl == 5){
					r = FromHex (text [1]);
					g = FromHex (text [2]);
					b = FromHex (text [3]);
					a = tl == 5 ? FromHex (text [4]) : 15;
					
					r = r << 4 | r;
					g = g << 4 | g;
					b = b << 4 | b;
					a = a << 4 | a;
				} else if (tl == 7 || tl == 9){
					r = FromHex (text [1]) << 4 | FromHex (text [2]);
					g = FromHex (text [3]) << 4 | FromHex (text [4]);
					b = FromHex (text [5]) << 4 | FromHex (text [6]);
					a = tl == 9 ? FromHex (text [7]) << 4 | FromHex (text [8]) : 255;
				} else {
					ColorError (text);
					return UIColor.Black;
				}
				return UIColor.FromRGBA (r, g, b, a);
			}
			ColorError (text);
			return UIColor.Black;
		}
		
		static UILineBreakMode ToLinebreakMode (string mode)
		{
			switch (mode){
			case "character-wrap": return UILineBreakMode.CharacterWrap;
			case "clip": return UILineBreakMode.Clip;
			case "head-truncation": return UILineBreakMode.HeadTruncation;
			case "middle-truncation": return UILineBreakMode.MiddleTruncation;
			case "tail-truncation": return UILineBreakMode.TailTruncation;
			case "word-wrap": return UILineBreakMode.WordWrap;
			default:
				Console.WriteLine ("Unexpeted linebreak mode `{0}', valid values include: character-wrap, clip, head-truncation, middle-truncation, tail-truncation and word-wrap", mode);
				return UILineBreakMode.Clip;
			}
		}
		
		// Parses a font in the format:
		// Name[-SIZE]
		// if -SIZE is omitted, then the value is SystemFontSize
		//
		static UIFont ToFont (string kvalue)
		{
			int q = kvalue.LastIndexOf ("-");
			string fname = kvalue;
			float fsize = 0;
			
			if (q != -1) {
				float.TryParse (kvalue.Substring (q+1), out fsize);
				fname = kvalue.Substring (0, q);
			}
			if (fsize <= 0)
				fsize = UIFont.SystemFontSize;

			var f = UIFont.FromName (fname, fsize);
			if (f == null)
				return UIFont.SystemFontOfSize (12);
			return f;
		}
		
		static UITableViewCellStyle ToCellStyle (string style)
		{
			switch (style){
			case "default": return UITableViewCellStyle.Default;
			case "subtitle": return UITableViewCellStyle.Subtitle;
			case "value1": return UITableViewCellStyle.Value1;
			case "value2": return UITableViewCellStyle.Value2;
			default:
				Console.WriteLine ("unknown cell style `{0}', valid values are default, subtitle, value1 and value2", style);
				break;
			}
			return UITableViewCellStyle.Default;
		}
		
		static UITextAlignment ToAlignment (string align)
		{
			switch (align){
			case "center": return UITextAlignment.Center;
			case "left": return UITextAlignment.Left;
			case "right": return UITextAlignment.Right;
			default:
				Console.WriteLine ("Unknown alignment `{0}'. valid values are left, center, right", align);
				return UITextAlignment.Left;
			}
		}
		
		//
		// Creates one of the various StringElement classes, based on the
		// properties set.   It tries to load the most memory efficient one
		// StringElement, if not, it fallsback to MultilineStringElement or
		// StyledStringElement
		//
		static Element LoadString (JsonObject json, object data)
		{
			string value = null;
			string caption = value;
			string background = null;
			NSAction ontap = null;
			NSAction onaccessorytap = null;
			int? lines;
			UITableViewCellAccessory? accessory;
			UILineBreakMode? linebreakmode;
			UITextAlignment? alignment;
			UIColor textcolor = null;
			UIFont font = null;
			UIFont subtitlefont = null;
			UITableViewCellStyle style = UITableViewCellStyle.Value1;

			foreach (var kv in json){
				string kvalue = (string) kv.Value;
				switch (kv.Key){
				case "caption":
					caption = kvalue;
					break;
				case "value":
					value = kvalue;
					break;
				case "background":	
					background = kvalue;
					break;
				case "style":
					style = ToCellStyle (kvalue);
					break;
				case "ontap": case "onaccessorytap":
					string sontap = kvalue;
					int p = sontap.LastIndexOf ('.');
					if (p == -1)
						break;
					NSAction d = delegate {
						string cname = sontap.Substring (0, p);
						string mname = sontap.Substring (p+1);
						var mi = Type.GetType (cname).GetMethod (mname, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
						if (mi != null)
							mi.Invoke (null, new object [] { data });
					};
					if (kv.Key == "ontap")
						ontap = d;
					else
						onaccessorytap = d;
					break;
				case "lines":
					int res;
					if (int.TryParse (kvalue, out res))
						lines = res;
					break;
				case "accessory":
					accessory = ToAccessory (kvalue);
					break;
				case "textcolor":
					textcolor = ParseColor (kvalue);
					break;
				case "linebreak":
					linebreakmode = ToLinebreakMode (kvalue);
					break;
				case "font":
					font = ToFont (kvalue);
					break;
				case "subtitlefont":
					subtitlefont = ToFont (kvalue);
					break;
				case "alignment":
					alignment = ToAlignment (kvalue);
					break;
					
				}
			}
			if (caption == null)
				caption = "";
			if (font != null || style != UITableViewCellStyle.Value1 || subtitlefont != null || linebreakmode.HasValue || textcolor != null || accessory.HasValue || onaccessorytap != null || background != null){
				StyledStringElement styled;
				
				if (lines.HasValue){
					styled = new StyledMultilineElement (caption, value, style);
					styled.Lines = lines.Value;
				} else {
					styled = new StyledStringElement (caption, value, style);
				}
				if (ontap != null)
					styled.Tapped += ontap;
				if (onaccessorytap != null)
					styled.AccessoryTapped += onaccessorytap;
				styled.Font = font;
				styled.SubtitleFont = subtitlefont;
				styled.TextColor = textcolor;
				if (accessory.HasValue)
					styled.Accessory = accessory.Value;
				if (linebreakmode.HasValue)
					styled.LineBreakMode = linebreakmode.Value;
				if (background != null){
					if (background.Length > 1 && background [0] == '#')
						styled.BackgroundColor = ParseColor (background);
					else
						styled.BackgroundUri = new Uri (background);
				}
				if (alignment.HasValue)
					styled.Alignment = alignment.Value;
				return styled;
			} else {
				StringElement se;
				if (lines == 0)
					se = new MultilineElement (caption, value);
				else
					se = new StringElement (caption, value);
				if (alignment.HasValue)
					se.Alignment = alignment.Value;
				if (ontap != null)
					se.Tapped += ontap;
				return se;
			}
		}
		
		static void LoadSectionElements (Section section, JsonArray array, object data)
		{
			if (array == null)
				return;
			
			for (int i = 0; i < array.Count; i++){
				Element element = null;
				
				try {
					var json = array [i] as JsonObject;
					if (json == null)
						continue;
					
					var type = GetString (json, "type");
					switch (type){
					case "bool": case "boolean":
						element = LoadBoolean (json);
						break;
						
					case "entry": case "password":
						element = LoadEntry (json, type == "password");
						break;
						
					case "string":
						element = LoadString (json, data);
						break;
						
					default:
						Error ("json element at {0} contain an unknown type `{1}', json {2}", i, type, json);
						break;
					}
				} catch (Exception e) {
					Console.WriteLine ("Error processing Json {0}, exception {1}", array, e);
				}
				if (element != null)
					section.Add (element);
			}
		}
	}
}

