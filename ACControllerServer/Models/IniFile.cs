using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ACControllerServer.Models
{

	public class IniFile
	{
		private string _path;

		public string Path
		{
			get
			{
				return _path;
			}
			set
			{
				_path = value;
			}
		}

		public bool Exists
		{
			get
			{
				if (_path != null)
                    return File.Exists(_path);
                return false;
			}
		}

		public IniFile(string INIPath)
		{
			_path = INIPath;
		}

		[DllImport("KERNEL32.DLL", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		private static extern int GetPrivateProfileStringW(string lpAppName, string lpKeyName, string lpDefault, string actualValue, int nSize, string lpFilename);

		[DllImport("KERNEL32.DLL", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		private static extern int WritePrivateProfileStringW(string lpAppName, string lpKeyName, string lpString, string lpFilename);

		public void IniWriteValue(string Section, string Key, string Value)
		{
			Key = Key?.ToUpper();
			Section = Section?.ToUpper();
			if (Value != null)
			{
				byte[] b2 = Encoding.UTF8.GetBytes(Value);
				Value = Encoding.Default.GetString(b2);
			}
			if (Key != null)
			{
				byte[] b3 = Encoding.UTF8.GetBytes(Key);
				Key = Encoding.Default.GetString(b3);
			}
			if (Section != null)
			{
				byte[] b = Encoding.UTF8.GetBytes(Section);
				Section = Encoding.Default.GetString(b);
			}
			if (WritePrivateProfileStringW(Section, Key, Value, _path) == 0)
			{
				Log.Information("Cannot write to: " + _path + "\n Error code: " + Marshal.GetLastWin32Error());
				throw new AccessViolationException("Cannot write to:\n" + _path + "\n\n Error code: " + Marshal.GetLastWin32Error());
			}
		}

		public void IniWriteValue(string section, string key, double value)
		{
			IniWriteValue(section.ToUpper(), key.ToUpper(), value.ToString(CultureInfo.GetCultureInfo("en-US")));
		}

		public string IniReadValue(string Section, string Key)
		{
			if (Key != null)
			{
				byte[] c2 = Encoding.UTF8.GetBytes(Key);
				Key = Encoding.Default.GetString(c2);
			}
			if (Section != null)
			{
				byte[] c = Encoding.UTF8.GetBytes(Section);
				Section = Encoding.Default.GetString(c);
			}
			string returnstr = new string(' ', 65535);
			int i = GetPrivateProfileStringW(Section, Key, "", returnstr, 65535, _path);
			byte[] b = Encoding.Default.GetBytes(returnstr.Trim());
			returnstr = Encoding.UTF8.GetString(b);
			returnstr = returnstr.Replace("\0", "").Trim();
			if (returnstr.Contains(";"))
			{
				returnstr = returnstr.Remove(returnstr.IndexOf(";", StringComparison.Ordinal)).Trim();
			}
			return returnstr;
		}

		public string IniReadValue(string Section, string Key, string DefaultValue)
		{
			string val = IniReadValue(Section, Key);
			return (val == "") ? DefaultValue : val;
		}

		public bool IniAssertValue(string section, string key, double compareto)
		{
			return IniReadDouble(section, key) == compareto;
		}

		public bool IniAssertValue(string section, string key, string compareto)
		{
			return IniReadValue(section, key) == compareto;
		}

		public int IniReadInt32(string section, string key)
		{
			string value = IniReadValue(section, key).Replace(",", ".");
			int output = 0;
			int.TryParse(value, out output);
			return output;
		}

		public double IniReadDouble(string section, string key)
		{
			string value = IniReadValue(section, key).Replace(",", ".");
			double output = 0.0;
			double.TryParse(value, out output);
			return output;
		}

		public List<string> GetKeys(string category)
		{
			string returnstr = new string(' ', 65535);
			GetPrivateProfileStringW(category, null, null, returnstr, 32768, _path);
			returnstr = returnstr.Trim().ToUpper();
			List<string> result = new List<string>(returnstr.Split(default(char)));
			result.RemoveRange(result.Count - 2, 2);
			return result;
		}

		public List<string> GetCategories()
		{
			string returnstr = new string(' ', 65535);
			int i = GetPrivateProfileStringW(null, null, null, returnstr, 65535, _path);
			byte[] encodedBytes = new UTF8Encoding().GetBytes(returnstr);
			returnstr = Encoding.UTF8.GetString(encodedBytes, 0, encodedBytes.Length);
			returnstr = returnstr.Trim().ToUpper();
			List<string> result = new List<string>(returnstr.Split(default(char)));
			result.RemoveRange(result.Count - 2, 2);
			return result;
		}

		public string GetStrCategories()
		{
			string returnstr = new string(' ', 65535);
			int i = GetPrivateProfileStringW(null, null, null, returnstr, 65535, _path);
			byte[] encodedBytes = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage).GetBytes(returnstr);
			returnstr = Encoding.UTF8.GetString(encodedBytes, 0, encodedBytes.Length);
			return CultureInfo.CurrentCulture.TextInfo.ANSICodePage + " // " + returnstr.Trim();
		}
	}
}
