using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CsQuery;

namespace DesuraDumper
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Desura Collection Dumper");
			Console.WriteLine ("(C) cyanic");
			Console.WriteLine ();

			// Variables
			string dbPath = null;
			string downloadsPath = null;
			string keysPath = null;
			string linksPath = null;
			string tokensPath = null;
			string keysExportFilter = null;
			bool exportDownloads = false;
			bool exportKeys = false;
			bool exportLinks = false;
			bool omitLinksComments = false;

			// Argument parser
			foreach (string arg in args) {
				// Each switch is in the format of "/option=value"

				if (arg.StartsWith ("/")) { // A switch
					string[] argSplit = arg.Split (new char[] { '=' }, 2);
					switch (argSplit [0].ToLower ()) { // Check option. New: Now works with flag options (no "=value").
					case "/i":
						// Check if specified already. Such a check should be present for every option.
						if (!string.IsNullOrEmpty (dbPath))
							argError ("Database path is specified more than once.");
						if (argSplit.Length != 2)
							argError ("Value for /i not specified.");
						dbPath = argSplit [1];
						break;
					case "/d":
						// Check if specified already. Such a check should be present for every option.
						if (!string.IsNullOrEmpty (downloadsPath))
							argError ("Downloads database path is specified more than once.");
						if (argSplit.Length != 2)
							argError ("Value for /d not specified.");
						downloadsPath = argSplit [1];
						break;
					case "/k":
						// Check if specified already. Such a check should be present for every option.
						if (!string.IsNullOrEmpty (dbPath))
							argError ("Keys database path is specified more than once.");
						if (argSplit.Length != 2)
							argError ("Value for /k not specified.");
						keysPath = argSplit [1];
						break;
					case "/l":
						// Check if specified already. Such a check should be present for every option.
						if (!string.IsNullOrEmpty (dbPath))
							argError ("Links file path is specified more than once.");
						if (argSplit.Length != 2)
							argError ("Value for /l not specified.");
						linksPath = argSplit [1];
						break;
					case "/t":
						// Check if specified already. Such a check should be present for every option.
						if (!string.IsNullOrEmpty (dbPath))
							argError ("Token path is specified more than once.");
						if (argSplit.Length != 2)
							argError ("Value for /t not specified.");
						tokensPath = argSplit [1];
						break;
					case "/xd":
						if (exportDownloads)
							argError ("Export downloads database option specified more than once.");
						exportDownloads = true;
						break;
					case "/xk":
						if (exportKeys)
							argError ("Export keys database option specified more than once.");
						exportKeys = true;
						if (argSplit.Length == 2)
							keysExportFilter = argSplit [1];
						break;
					case "/xl":
						if (exportLinks)
							argError ("Export links option specified more than once.");
						exportLinks = true;
						break;
					case "/o":
						if (omitLinksComments)
							argError ("Omit links file comments option specified more than once.");
						omitLinksComments = true;
						break;
					case "/?":
					case "/h":
					case "/help":
						usage ();
						break;
					default:
						argError ("Unknown argument " + argSplit [0]);
						break;
					}
				}
			}

			// Fill in default paths, if not specified
			if (dbPath == null)
				dbPath = "database.yml";
			if (downloadsPath == null)
				downloadsPath = "downloads.yml";
			if (keysPath == null)
				keysPath = "keys.yml";
			if (linksPath == null)
				linksPath = "downloadLinks.txt";
			if (tokensPath == null)
				tokensPath = "tokens.txt";
			if (keysExportFilter == null)
				keysExportFilter = ".*";

			// Argument validation. Oh boy...
			if ((exportKeys || exportDownloads) && !File.Exists (dbPath))
				argError ("Input database must be specified.");
			if (exportLinks && !File.Exists (dbPath) && !File.Exists (downloadsPath))
				argError ("Either main database or downloads database path must be specified.");
			if (!exportKeys && !exportDownloads && !exportLinks) {
				// Running with no export arguments, which auto-exports keys and downloads
				exportKeys = exportDownloads = true;
			}

			try {
				CookieAwareWebClient wc = new CookieAwareWebClient () { Encoding = Encoding.UTF8 };

				if (exportLinks || !File.Exists (dbPath)) {
					if (!PromptAndLogIn (wc, tokensPath))
						return;
				}
				
				List<ProductInfo> products = null;
				if (!(exportLinks && File.Exists (downloadsPath))) {
					if (!File.Exists (dbPath)) {
						products = DownloadInitial (wc);
						Console.Error.WriteLine ("Saving database...");
						using (StreamWriter sw = File.CreateText (dbPath)) {
							ProductInfo.SerializeCollection (products, sw);
							sw.Flush ();
						}
					} else {
						Console.Error.WriteLine ("Loading database...");
						using (StreamReader sr = File.OpenText (dbPath)) {
							products = ProductInfo.DeserializeCollection (sr);
						}
					}
				}

				List<DownloadsView> downloadsDb = null;
				if (products != null)
					downloadsDb = DownloadsView.CreateCollectionFromProducts (products);

				if (exportDownloads) {
					Console.Error.WriteLine ("Exporting downloads database...");
					using (StreamWriter sw = File.CreateText (downloadsPath)) {
						DownloadsView.SerializeCollection (downloadsDb, sw);
						sw.Flush ();
					}
				}

				if (exportKeys) {
					Console.Error.WriteLine ("Exporting keys database...");
					using (StreamWriter sw = File.CreateText (keysPath)) {
						KeysView.SerializeCollection (KeysView.CreateCollectionFromProducts (products, keysExportFilter), sw);
						sw.Flush ();
					}
				}

				if (exportLinks) {
					if (downloadsDb == null) {
						// Deserialize downloads DB
						Console.Error.WriteLine ("Loading downloads database...");
						using (StreamReader sr = File.OpenText (downloadsPath)) {
							downloadsDb = DownloadsView.DeserializeCollection (sr);
						}
					}

					List<string> links = GetCdnLinks (downloadsDb, wc, !omitLinksComments);

					Console.Error.WriteLine ("Saving links to file...");
					using (StreamWriter sw = File.CreateText (linksPath)) {
						foreach (string link in links) {
							sw.WriteLine (link);
						}
						sw.Flush ();
					}
				}

				Console.Error.WriteLine ("Done.");
			} catch (Exception ex) {
				Console.Error.WriteLine ("Error during processing: {0}", ex.Message);
				Console.Error.WriteLine (ex);
			}
		}

		static List<string> GetCdnLinks (List<DownloadsView> downloads, CookieAwareWebClient wc, bool addComments = true)
		{
			List<string> links = new List<string> ();
			for (int i = 0; i < downloads.Count; ++i) {
				DownloadsView view = downloads [i];
				Console.WriteLine ("[{0}/{1}] {2}: Getting CDN links", i + 1, downloads.Count, view.Name);

				List<string> presentNames = new List<string> (view.Downloads.Select (v => v.Name + "\t" + v.Platform));
				List<Tuple<string, string, string>> prodDownloads = GetDownloads (view.Slug, view.BranchId, wc);
				foreach (Tuple<string, string, string> tuple in prodDownloads.Where(t => presentNames.Contains(t.Item1 + "\t" + t.Item2))) {
					if (addComments)
						links.Add (string.Format ("# {0} - {1} ({2})", view.Name, tuple.Item1, tuple.Item2));
					links.Add (GetRedirectUrl (tuple.Item3, wc.CookieContainer));
				}
			}


			return links;
		}

		static void RefreshProductWithDownloads (ProductInfo product, WebClient wc)
		{
			var downloads = GetDownloads (product.Slug, product.BranchId, wc);
			foreach (var info in downloads) {
				NamePlatformStringTuple existing = product.Downloads.Find (t => t.Name == info.Item1 && t.Platform == info.Item2);
				if (existing != null)
					product.Downloads.Remove (existing);
				product.Downloads.Add (new NamePlatformStringTuple (info.Item1, info.Item2));
			}
		}

		static List<ProductInfo> DownloadInitial (WebClient wc)
		{
			Console.WriteLine ("Downloading collection...");
			dynamic coll = JObject.Parse (wc.DownloadString ("http://www.desura.com/games/ajax/json/all?collection=t"));
			//dynamic coll = JObject.Parse (File.ReadAllText ("dump.json"));
			if (coll.error != false || coll.success != true) {
				Console.Error.WriteLine ("Failed to load collection.");
				return null;
			}

			List<ProductInfo> products = ProductInfo.CreateFromCollection ((JArray)coll.games);

			for (int i = 0; i < products.Count; ++i) {
				ProductInfo product = products [i];
				Console.WriteLine ("[{0}/{1}] {2}: Getting download links", i + 1, products.Count, product.Name);
				RefreshProductWithDownloads (product, wc);
				Console.WriteLine ("[{0}/{1}] {2}: Getting keys", i + 1, products.Count, product.Name);
				AddKeysToProduct (product, wc);
			}

			return products;
		}

		static List<Tuple<string, string, string>> GetDownloads (string slug, int branchId, WebClient wc)
		{
			List<Tuple<string, string, string>> downloads = new List<Tuple<string, string, string>> ();
			// Get downloads
			string downloadsPage = wc.DownloadString (string.Format ("http://www.desura.com/games/{0}/download/{1}", slug, branchId));
			CQ csqDownload = downloadsPage;
			CQ dSearch = csqDownload ["span.action:contains(Download)"];

			foreach (var dSpanE in dSearch) {
				CQ dSpan = dSpanE.Cq ();
				CQ parentA = dSpan.Closest ("a");
				// Get download link
				string link = "http://www.desura.com" + parentA.Attr ("href");

				// Get download name
				CQ infoBlock = parentA.Find (".buycontent .heading.clear");
				downloads.Add (new Tuple<string, string, string> (infoBlock.Children ().Eq (0).Text (), infoBlock.Children ().Eq (1).Text (), link));			
			}

			return downloads;
		}

		static string GetRedirectUrl (string url, CookieContainer cookies)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.CreateHttp (url);
			request.AllowAutoRedirect = false;
			request.CookieContainer = cookies;
			request.Method = WebRequestMethods.Http.Head;
			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse ()) {
				if (response.StatusCode == HttpStatusCode.Found || response.StatusCode == HttpStatusCode.Moved)
					return response.Headers [HttpResponseHeader.Location];
				else
					return url;
			}
		}

		static void AddKeysToProduct (ProductInfo product, WebClient wc)
		{
			// Get keys
			string keysPage = wc.DownloadString (string.Format ("http://www.desura.com/games/{0}/keys", product.Slug));
			CQ csqKeys = keysPage;
			CQ kSearch = csqKeys ["span.price:contains(Select),span.price:contains(Get)"];
			foreach (var kSpanE in kSearch) {
				CQ kSpan = kSpanE.Cq ();
				CQ parentSpan = kSpan.Closest ("span.buy.clear");
				// Get key name
				string keyType = kSpan.Parent ().Find (".action").Text ();
				CQ infoBlock = parentSpan.Find (".buycontent .heading.clear");
				string keyName = infoBlock.Children ().Eq (0).Text ();
				string name = string.Format ("{0} ({1})", keyName, keyType);
				// Get key
				string key = kSpan.Text () == "Select" ? parentSpan.Find (".summary [name=\"key\"]").Attr ("value") : string.Format ("Reveal key at http://www.desura.com/games/{0}/keys", product.Slug);

				product.Keys.Add (new NameKeyStringTuple (name, key));
			}
		}

		static bool PromptAndLogIn (CookieAwareWebClient wc, string tokenPath)
		{
			if (File.Exists (tokenPath)) {
				Console.Error.WriteLine ("Found existing tokens, using them.");
				Uri desuraUri = new Uri ("http://www.desura.com");
				CookieContainer container = wc.CookieContainer;
				using (StreamReader sr = File.OpenText (tokenPath)) {
					while (!sr.EndOfStream) {
						string[] line = sr.ReadLine ().Split ('=');
						container.Add (desuraUri, new Cookie (line [0], line [1]));
					}
				}
			} else {
				Console.WriteLine ("Log in");
				Console.Write ("Username/email address: ");
				string username = Console.ReadLine ();
				Console.Write ("Password: ");
				string pass = ReadPassword ();
				if (username.Length == 0) {
					Console.WriteLine ("Empty email address");
					return false;
				}
				if (pass.Length == 0) {
					Console.WriteLine ("Empty password");
					return false;
				}

				Console.WriteLine ("Logging in...");

				// Make args
				var form = System.Web.HttpUtility.ParseQueryString (string.Empty);
				form ["referer"] = "http://www.desura.com";
				form ["username"] = username;
				form ["password"] = pass;
				form ["rememberme"] = "1";
				form ["members"] = "Sign In";

				wc.Headers.Add (HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");
				string result = wc.UploadString ("https://secure.desura.com/members/login", form.ToString ());
				if (result.Contains ("Login to Desura and enter a world of games to play, friends to challenge and community groups to join.")) {
					Console.Error.WriteLine ("Login failed.");
					return false;
				}

				Console.Error.WriteLine ("Saving tokens...");
				try {
					using (StreamWriter sw = File.CreateText (tokenPath)) {					
						foreach (Cookie cookie in wc.CookieContainer.GetCookies(new Uri("http://www.desura.com"))) {
							sw.WriteLine (cookie.ToString ());
						}
						sw.Flush ();
					}
				} catch (Exception ex) {
					Console.Error.WriteLine ("Error saving tokens ({0}), carrying on", ex.Message);
				}
			}

			return true;
		}

		static void argError (string message)
		{
			Console.Error.WriteLine (message);
			Console.Error.WriteLine ();
			usage ();
		}

		static void usage ()
		{
			Console.WriteLine ("{0} [/i=dbPath] [/d=downloadsPath] [/k=keysPath] [/l=linksPath] [/t=tokensPath] [/xd] [/xk[=filter]] [/xl [/o]]", Path.GetFileNameWithoutExtension (System.Reflection.Assembly.GetExecutingAssembly ().Location));
			Console.WriteLine ("\t/i\tPath to main database. Default: database.yml");
			Console.WriteLine ("\t/d\tPath to downloads database. Default: downloads.yml");
			Console.WriteLine ("\t/k\tPath to keys database. Default: keys.yml");
			Console.WriteLine ("\t/l\tPath to CDN links file. Default: downloadLinks.txt");
			Console.WriteLine ("\t/t\tPath to tokens file. Default: tokens.txt");
			Console.WriteLine ("\t/xd\tExport to downloads database. Main database must exist.");
			Console.WriteLine ("\t/xk\tExport to keys database. Main database must exist. A regex filter can also be specified to export certain types of keys.");
			Console.WriteLine ("\t/xl\tExport to CDN links file. Main database or downloads database must exist.");
			Console.WriteLine ("\t/o\tOmit comments when writing to links file (for e.g. wget).");
			Console.WriteLine ();
			Console.WriteLine ("Provide no arguments to create main, downloads, and keys database from your Desura collection.");
			Environment.Exit (0);
		}

		// https://stackoverflow.com/a/7049688/1180879

		/// <summary>
		/// Like System.Console.ReadLine(), only with a mask.
		/// </summary>
		/// <param name="mask">a <c>char</c> representing your choice of console mask</param>
		/// <returns>the string the user typed in </returns>
		static string ReadPassword (char mask)
		{
			// Modified for UNIX LF
			const int ENTER = 10 , BACKSP= 8 , CTRLBACKSP= 127;
			int[] FILTERED = { 0, 27, 9, 10, 13 /*, 32 space, if you care */ }; // const

			var pass = new Stack<char> ();
			char chr = (char)0;

			while ((chr = System.Console.ReadKey (true).KeyChar) != ENTER && chr != 13) {
				if (chr == BACKSP || chr == 0) { // For some reason on my Ubuntu terminal backspace is a null char
					if (pass.Count > 0) {
						System.Console.Write ("\b \b");
						pass.Pop ();
					}
				} else if (chr == CTRLBACKSP) {
					while (pass.Count > 0) {
						System.Console.Write ("\b \b");
						pass.Pop ();
					}
				} else if (FILTERED.Count (x => chr == x) > 0) {
				} else {
					pass.Push ((char)chr);
					System.Console.Write (mask);
				}
			}

			System.Console.WriteLine ();

			return new string (pass.Reverse ().ToArray ());
		}

		/// <summary>
		/// Like System.Console.ReadLine(), only with a mask.
		/// </summary>
		/// <returns>the string the user typed in </returns>
		static string ReadPassword ()
		{
			return ReadPassword ('*');
		}

		// https://stackoverflow.com/a/732110/1180879
		static string StripHTML (string HTMLText, bool decode = true)
		{
			Regex reg = new Regex ("<[^>]+>", RegexOptions.IgnoreCase);
			var stripped = reg.Replace (HTMLText, "");
			return decode ? System.Web.HttpUtility.HtmlDecode (stripped) : stripped;
		}
	}
}
