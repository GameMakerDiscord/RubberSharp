using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoYoProject;
using YoYoProject.Controllers;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using System.Diagnostics;

namespace RubberSharp {
	sealed class RubberCommandline {
		public const string Version = "1.0.0.0";
		static void Main(string[] args) {
			Console.Write("Starting ");
			ColorConsole.Write(ConsoleColor.Green, "Rubber# ");
			Console.Write("An IGOR Wrapper\n");

			// Check ARGS length, if bad show usage
			if(args.Length <= 2) {
				DisplayUsage();
				return;
			}

            RubberConfig config = new RubberConfig() {
                Config = "default"
            };

			// Parse Args
			config.ProjectPath = args[0];
			config.ExportPlatform = Rubber.ExportPlatformFromString(args[1]);
			config.ExportType = Rubber.ExportTypeFromString(args[2]);

            switch(config.ExportType) {
                case ExportType.PackageNsis:
                case ExportType.PackageZip:
                    if(args.Length <= 3) {
                        DisplayUsage();
                        return;
                    }
                    config.ExportOutputLocation = args[3];
                    break;
                case ExportType.Run:
                    // no path used
                    break;
            }
            
			for(int i = 3; i < args.Length; i++) {
				switch(args[i]) {
					case "-debug":
						config.Debug = true;
						break;
					case "-verbose":
						config.Verbose = true;
						break;
					case "-config":
						if(i != args.Length) {
							i++;
							config.Config = args[i];
						}
						break;
				}
			}

			Rubber.Compile(config);
		}
		static void DisplayUsage() {
			ColorConsole.Write(ConsoleColor.Red, "Usage: ");
			Console.Write(System.AppDomain.CurrentDomain.FriendlyName + " <project> <platform> <export type> [export path] [-flags]");
			Console.WriteLine();
			Console.WriteLine("Flags");
			Console.WriteLine("  -debug                  Compile with debug mode enabled");
			Console.WriteLine("  -verbose                Enable verbose on IGOR");
			Console.WriteLine("  -config <config-name>   Set the config  ");
			return;
		}
	}
	public class Rubber {
		public const string Version = "1.0.0.0";
		public static int Compile(RubberConfig config) {
			string TempUID = "";
			try {
				var r = new Random();
				TempUID = "rubber_" + r.Next(0x0, 0xFFFFFF).ToString("X");
                string TempPath = Path.Combine(Directory.GetCurrentDirectory(), TempUID);
				string GMCache = Path.Combine(TempPath, "GMCache");
				string GMTemp = Path.Combine(TempPath, "GMTemp");
				string GMOutput = Path.Combine(TempPath, "GMOutput");

				Directory.CreateDirectory(TempPath);
				Directory.CreateDirectory(GMCache);
				Directory.CreateDirectory(GMTemp);
				Directory.CreateDirectory(GMOutput);

				GMProject project = GMProject.Load(config.ProjectPath);

				if(config.ExportType == ExportType.Run) config.ExportOutputLocation = "";

				string gm_appdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameMakerStudio2");

				Dictionary<string, string> umjson = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(gm_appdata, "um.json")));

				string um_user = umjson["username"];
				string um_user_id = umjson["userID"];

				string userDir = Path.Combine(gm_appdata, um_user.Substring(0, um_user.IndexOf("@")) + "_" + um_user_id);
				Dictionary<string, string> runtimes = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(Environment.GetEnvironmentVariable("ProgramData"), "GameMakerStudio2", "runtime.json")));

				string runtimeLocation = runtimes[runtimes["active"]];
				runtimeLocation = runtimeLocation.Substring(0, runtimeLocation.IndexOf("&"));

				// Generate Files
				File.WriteAllText(Path.Combine(TempPath, "build.bff"), new Models.BFF() {
					// Directly From `config`
					targetFile = config.ExportOutputLocation,
					debug = config.Debug ? "True" : "False",
					verbose = config.Verbose ? "True" : "False",
					config = config.Config,

					outputFolder = GMOutput,
					compile_output_file_name = Path.Combine(GMOutput, Path.GetFileName(project.RootDirectory) + ".win"),
					projectName = Path.GetFileName(project.RootDirectory),
					projectPath = Path.Combine(project.RootDirectory, Path.GetFileName(project.RootDirectory) + ".yyp"),
					projectDir = project.RootDirectory,
					applicationPath = Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432"), "GameMaker Studio 2", "GameMakerStudio.exe"),
					runtimeLocation = runtimeLocation,
					userDir = userDir,
					tempFolder = Path.Combine(TempPath, "GMTemp"),

					// Other JSONBuildFiles
					macros = Path.Combine(TempPath, "macros.json"),
					targetOptions = Path.Combine(TempPath, "targetoptions.json"),
					preferences = Path.Combine(TempPath, "preferences.json"),
					steamOptions = Path.Combine(TempPath, "steam_options.yy"),

					// Values that are always the same
					assetCompiler = "",
					useShaders = "True",
					targetMask = "64",
					helpPort = "51290",
					debuggerPort = "6509"
				}.Serialize());
				File.WriteAllText(Path.Combine(TempPath, "macros.json"), new Models.Macros() {
					UserProfileName = Environment.UserName,
					project_dir = project.RootDirectory,
					project_name = Path.GetFileName(project.RootDirectory),
					daveead_cd = Directory.GetCurrentDirectory(),
					daveead_tempID = TempUID,
					runtimeLocation = runtimeLocation
				}.Serialize());
				File.WriteAllText(Path.Combine(TempPath, "steam_options.yy"), new Models.SteamOptions() {
					steamsdk_path = RubberUtilities.GetPreference("machine.Platform Settings.Steam.steamsdk_path"),
				}.Serialize());
				File.WriteAllText(Path.Combine(GMCache, "MainOptions.json"), project.Resources.Get<GMMainOptions>().ToJson()); // this is boke right now

				switch(config.ExportPlatform) {
					case ExportPlatform.Windows:
						File.WriteAllText(Path.Combine(TempPath, "preferences.json"), new Models.PreferencesWindows() {
							default_packaging_choice = RubberUtilities.GetPreference("machine.Platform Settings.Windows.choice"),
							visual_studio_path = RubberUtilities.GetPreference("machine.Platform Settings.Windows.visual_studio_path")
						}.Serialize());
						File.WriteAllText(Path.Combine(TempPath, "targetoptions.json"), new Models.TargetOptionsWindows() {
							runtime = config.YYC ? "YYC" : "VM"
						}.Serialize());
						File.WriteAllText(Path.Combine(GMCache, "PlatformOptions.json"), project.Resources.Get<GMWindowsOptions>().ToJson());
						break;
					case ExportPlatform.Mac:
						throw new NotImplementedException("MacOS Export has not been added yet");
					case ExportPlatform.Linux:
						throw new NotImplementedException("Linux/Ubuntu Export has not been added yet");
				}

				var process = new Process {
					StartInfo = new ProcessStartInfo {
						FileName = Path.Combine(runtimeLocation, "bin", "igor.exe"),
						Arguments = "-options=\"" + Path.Combine(TempPath, "build.bff") + "\" -- " + config.ExportPlatform.ToString() + " " + config.ExportType.ToString(),
						CreateNoWindow = true,
						UseShellExecute = false,
						RedirectStandardOutput = true,
						RedirectStandardError = true
					}
				};

				process.OutputDataReceived += ReadOutput;
				process.ErrorDataReceived += ReadError;

				if(!process.Start())
					return process.ExitCode;

				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				process.WaitForExit();

                if(process.ExitCode < 0) throw new Exception("IGOR Failed");
				Directory.Delete(TempUID, true);
				return process.ExitCode;
			} catch(Exception e) {
				// throw and keep directory
				throw e;
			}
		}

		private static void ReadOutput(object sender, DataReceivedEventArgs e) {
			Console.WriteLine(e.Data);
		}
		private static void ReadError(object sender, DataReceivedEventArgs e) {
			ColorConsole.Write(ConsoleColor.Red, e.Data+"\n");
		}

		public static ExportPlatform ExportPlatformFromString(string str) {
			switch(str.ToLowerInvariant()) {
				case "windows":
					return ExportPlatform.Windows;

				case "mac":
				case "osx":
				case "macos":
					return ExportPlatform.Mac;

				case "linux":
				case "ubuntu":
					return ExportPlatform.Linux;

				default:
					throw new Exception(str + " is not a platform (thats supported)");
			}
		}
		public static ExportType ExportTypeFromString(string str) {
			switch(str.ToLowerInvariant()) {
				case "run":
				case "test":
				case "play":
					return ExportType.Run;
				case "zip":
				case "packagezip":
				case "zipfile":
				case "packagezipfile":
					return ExportType.PackageZip;
				case "installer":
				case "packagensis":
				case "nsis":
				case "packageinstaller":
					return ExportType.PackageNsis;

				default:
					throw new Exception(str + " is not a export type (thats supported)");
			}
		}
	}
	public sealed class RubberConfig {
		public string ProjectPath { get; set; }
		public ExportPlatform ExportPlatform { get; set; }
		public ExportType ExportType { get; set; }
		public string ExportOutputLocation { get; set; }
		public bool Verbose { get; set; }
		public bool YYC { get; set; }
		public bool Debug { get; set; }
		public string Config { get; set; }
	}
	public enum ExportType {
		PackageZip,
		/// <summary>aka the installer.</summary>
		PackageNsis,
		Run
	}
	public enum ExportPlatform {
		Windows,
		Mac,
		Linux,
	}
	namespace Models {
		// todo: setup mac and linux files
		public class JSONBuildFile {
			public string Serialize() {
				JavaScriptSerializer ser = new JavaScriptSerializer();
				return ser.Serialize(this);
			}
		}
		public sealed class BFF : JSONBuildFile {
			public string targetFile { get; set; }
			public string assetCompiler { get; set; }
			public string debug { get; set; }
			public string compile_output_file_name { get; set; }
			public string useShaders { get; set; }
			public string steamOptions { get; set; }
			public string config { get; set; }
			public string outputFolder { get; set; }
			public string projectName { get; set; }
			public string projectDir { get; set; }
			public string preferences { get; set; }
			public string projectPath { get; set; }
			public string userDir { get; set; }
			public string runtimeLocation { get; set; }
			public string applicationPath { get; set; }
			public string macros { get; set; }
			public string tempFolder { get; set; }
			public string targetOptions { get; set; }
			public string targetMask { get; set; }
			public string verbose { get; set; }
			public string helpPort { get; set; }
			public string debuggerPort { get; set; }
		}
		public sealed class SteamOptions : JSONBuildFile {
			public string steamsdk_path { get; set; }
		}
		public sealed class PreferencesWindows : JSONBuildFile {
			public string default_packaging_choice { get; set; }
			public string visual_studio_path { get; set; }
		}
		public sealed class PreferencesMac : JSONBuildFile {
			// todo
		}
		public sealed class PreferencesLinux : JSONBuildFile {
			// todo
		}
		public sealed class TargetOptionsWindows : JSONBuildFile {
			public string runtime { get; set; }
		}
		public sealed class TargetOptionsMac : JSONBuildFile {
			// todo
		}
		public sealed class TargetOptionsLinux : JSONBuildFile {
			// todo
		}
		public sealed class Macros : JSONBuildFile {
			public string daveead_cd { get; set; }
			public string daveead_tempID { get; set; }
			public string project_name { get; set; }
			public string project_dir { get; set; }
			public string UserProfileName { get; set; }

			public string daveead_tempdir = "${daveead_cd}\\${daveead_tempID}";
			public string daveead_gm_cache = "${daveead_tempdir}\\GMCache";
			public string daveead_gm_temp = "${daveead_tempdir}\\GMTemp";
			public string daveead_output = "${daveead_tempdir}\\GMOutput";

			public string project_full_filename = "${project_dir}\\${project_name}.yyp";
			public string options_dir = "${project_dir}\\options";

			public string project_cache_directory_name = "GMCache";
			public string asset_compiler_cache_directory = "${daveead_tempdir}";

			public string project_dir_inherited_BaseProject = "${runtimeLocation}\\BaseProject";
			public string project_full_inherited_BaseProject = "${runtimeLocation}\\BaseProject\\BaseProject.yyp";
			public string base_project = "${runtimeLocation}\\BaseProject\\BaseProject.yyp";
			public string base_options_dir = "${runtimeLocation}\\BaseProject\\options";

			public string local_directory = "${ApplicationData}\\${program_dir_name}";
			public string local_cache_directory = "${local_directory}\\Cache";
			public string temp_directory = "${daveead_gm_temp}";

			public string system_directory = "${CommonApplicationData}\\${program_dir_name}";
			public string system_cache_directory = "${system_directory}\\Cache";
			public string runtimeBaseLocation = "${system_cache_directory}\\runtimes";
			public string runtimeLocation = "";

			public string igor_path = "${runtimeLocation}\\bin\\Igor.exe";
			public string asset_compiler_path = "${runtimeLocation}\\bin\\GMAssetCompiler.exe";
			public string lib_compatibility_path = "${runtimeLocation}\\lib\\compatibility.zip";
			public string runner_path = "${runtimeLocation}\\windows\\Runner.exe";
			public string webserver_path = "${runtimeLocation}\\bin\\GMWebServer.exe";
			public string html5_runner_path = "${runtimeLocation}\\html5\\scripts.html5.zip";
			public string adb_exe_path = "platform-tools\\adb.exe";
			public string java_exe_path = "bin\\java.exe";
			public string licenses_path = "${exe_path}\\Licenses";

			public string keytool_exe_path = "bin\\keytool.exe";
			public string openssl_exe_path = "bin\\openssl.exe";

			public string program_dir_name = "GameMakerStudio2";
			public string program_name = "GameMakerStudio2";
			public string program_name_pretty = "GameMaker Studio 2";

			public string default_font = "Open Sans";
			public string default_style = "Regular";
			public string default_font_size = "9";

			public string ApplicationData = "${UserProfile}\\AppData\\Roaming";
			public string CommonApplicationData = "C:\\ProgramData";
			public string ProgramFiles = "C:\\Program Files";
			public string ProgramFilesX86 = "C:\\Program Files (x86)";
			public string CommonProgramFiles = "C:\\Program Files\\Common Files";
			public string CommonProgramFilesX86 = "C:\\Program Files (x86)\\Common Files";
			public string UserProfile = "C:\\Users\\${UserProfileName}";
			public string TempPath = "${UserProfile}\\AppData\\Local";
			public string exe_path = "${ProgramFiles}\\GameMaker Studio 2";
		}
	}
	public static class RubberUtilities {
		private static Dictionary<string, string> preferences = null;
		public static string GetPreference(string key) {
			if(preferences == null) {
				string gm_appdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameMakerStudio2");

				Dictionary<string, string> umjson = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(gm_appdata,"um.json")));

				string um_user = umjson["username"];
				string um_user_id = umjson["userID"];

				string preferences_file = Path.Combine(gm_appdata,um_user.Substring(0,um_user.IndexOf("@"))+"_"+um_user_id, "local_settings.json");
				preferences = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(preferences_file));
			}
			try {
				return preferences[key];
			} catch(Exception) {
				return "";
			}

		}
	}
}
