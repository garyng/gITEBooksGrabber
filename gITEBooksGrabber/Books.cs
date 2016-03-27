using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace gITEBooksGrabber
{
	[DataContract]
	public class Books
	{
		[DataMember]
		private Dictionary<int, Book> _dBooks = new Dictionary<int, Book>();

		private const string DB_FILENAME = "ebooks.json";

		public Dictionary<int, Book> Items
		{
			get
			{
				return _dBooks;
			}
			set
			{
				_dBooks = value;
			}
		}

		public async Task Start()
		{
			await start();
		}

		private async Task start()
		{
			await loadJSON(DB_FILENAME);
			await parseBooks();
		}

		private async Task parseBooks()
		{
			consoleWrite("Parsing Latest Upload ID...");

			int maxId = await parseLatestBookId();
			if (maxId == -1)
			{
				Console.WriteLine("Press any key to exit...");
				return;
			}
			consoleWrite(String.Format("Latest Upload ID : #{0}", maxId));
			Console.WriteLine();

			for (int i = 1; i <= maxId; i++)
			{
				consoleWrite(string.Format("Parsing Book #{0}", i));
				Console.Write("\r");
				Book b;
				if (!_dBooks.TryGetValue(i, out b))
				{
					b = await parseBookInfo(i);
					if (b != null)
					{
						await saveBook(b);
						_dBooks.Add(i, b);
						await saveJSON(DB_FILENAME);
					}
				}
			}
		}

		private async Task<int> parseLatestBookId()
		{
			try
			{
				string content = await (new WebClient()).DownloadStringTaskAsync("http://it-ebooks.info/");

				HtmlDocument hdoc = new HtmlDocument();
				hdoc.LoadHtml(content);

				string id = hdoc.DocumentNode.SelectSingleNode("//td[@width=120]/a").Attributes["href"].Value.Trim().Split('/')[2];
				return int.Parse(id);
			}
			catch (Exception ex)
			{
				Console.WriteLine("\rUnable to parse latest book id....");
				Console.WriteLine(ex.Message);
				return -1;
			}
		}

		private async Task<Book> parseBookInfo(int id)
		{
			try
			{
				Book b = new Book();
				b.ID = id.ToString();
				b.URL = "http://it-ebooks.info/book/" + b.ID;
				string book = await (new WebClient()).DownloadStringTaskAsync(b.URL);

				HtmlDocument hdoc = new HtmlDocument();
				hdoc.LoadHtml(book);

				b.Title = hdoc.DocumentNode.SelectSingleNode("//*[@itemprop='name']").InnerText.Trim();
				b.Title = sanitizePath(b.Title);
				b.Description = hdoc.DocumentNode.SelectSingleNode("//*[@itemprop='description']").InnerText.Trim();
				b.Publisher = hdoc.DocumentNode.SelectSingleNode("//*[@itemprop='publisher']").InnerText.Trim();
				b.Publisher = sanitizePath(b.Publisher);
				b.Author = hdoc.DocumentNode.SelectSingleNode("//*[@itemprop='author']").InnerText.Trim();
				b.ISBN = hdoc.DocumentNode.SelectSingleNode("//*[@itemprop='isbn']").InnerText.Trim();
				b.DatePublished = hdoc.DocumentNode.SelectSingleNode("//*[@itemprop='datePublished']").InnerText.Trim();
				b.NumberOfPages = hdoc.DocumentNode.SelectSingleNode("//*[@itemprop='numberOfPages']").InnerText.Trim();
				b.Language = hdoc.DocumentNode.SelectSingleNode("//*[@itemprop='inLanguage']").InnerText.Trim();
				b.Format = hdoc.DocumentNode.SelectSingleNode("//*[@itemprop='bookFormat']").InnerText.ToLower().Trim();
				b.DownloadLink = hdoc.DocumentNode.SelectNodes("//a").ToList().Where(item => item.Attributes["href"].Value.Contains("filepi.com")).First().Attributes["href"].Value;
				b.FileName = string.Format("{0}.{1}", b.Title, b.Format);
				b.SavePath = Path.Combine(b.Publisher, b.Title, b.FileName);
				b.Downloaded = false;
				return b;
			}
			catch (Exception ex)
			{
				Console.WriteLine("\rUnable to parse book info... (ID : #{0})", id);
				Console.WriteLine(ex.Message);
				return null;
			}
		}
		private string sanitizePath(string path)
		{
			char[] invalidChars = Path.GetInvalidFileNameChars();
			return String.Join("_", path.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
		}

		private async Task<bool> saveBook(Book b)
		{
			try
			{
				await downloadFile(b.DownloadLink, b.SavePath, b.URL, new Progress<string>(p =>
				{
					Console.Write(string.Format("\rDownloading Book #{0} : {1}", b.ID, b.Title).PadRight(Console.BufferWidth - p.Length - 1));
					Console.Write(p);
				}));
				b.Downloaded = true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(string.Format("\rUnable to download Book #{0} : {1}", b.ID, b.Title).PadRight(Console.BufferWidth));
				Console.WriteLine(ex.Message);

				b.Downloaded = false;
				return false;
			}
			return true;
		}

		private async Task downloadFile(string url, string filePath, string referer, IProgress<string> progress)
		{
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
			req.Referer = referer;

			using (HttpWebResponse res = (HttpWebResponse)await req.GetResponseAsync())
			{
				using (Stream stRemote = res.GetResponseStream())
				{
					Directory.CreateDirectory(Path.GetDirectoryName(filePath));

					using (Stream stLocal = File.Create(filePath))
					{
						long totalBytes = res.ContentLength;
						byte[] buffer = new byte[10240];
						int bytesRead = 0;
						long totalBytesRead = 0;
						do
						{
							bytesRead = await stRemote.ReadAsync(buffer, 0, buffer.Length);
							await stLocal.WriteAsync(buffer, 0, bytesRead);
							totalBytesRead += bytesRead;

							progress.Report(String.Format("{0}/{1} [{2:0.00}%]", convertBytesToString(totalBytesRead), convertBytesToString(totalBytes), (double)totalBytesRead * 100.00 / (double)totalBytes));

						} while (bytesRead > 0);
					}
				}
			}
		}

		private string convertBytesToString(long len)
		{
			double length = (double)len;
			string[] sizes = { "B", "KB", "MB", "GB", "TB" };
			int unit = 0;
			while (length > 1024 && unit + 1 < sizes.Length)
			{
				unit++;
				length /= 1024;
			}
			return String.Format("{0:0.00}{1}", length, sizes[unit]);
		}

		private void consoleWrite(string str)
		{
			Console.Write(String.Format("\r{0}", str).PadRight(Console.BufferWidth));
		}

		private async Task loadJSON(string filename)
		{
			consoleWrite("Loading database from JSON file...");
			if (File.Exists(filename))
			{
				string json = await Task.Run<string>(() =>
				{
					return File.ReadAllText(filename);
				});

				Books b = json.FromJson<Books>();
				_dBooks = b.Items;
				Console.WriteLine(String.Format("\rTotal books in databse : {0}", _dBooks.Count).PadRight(Console.BufferWidth));
			}

		}

		private async Task saveJSON(string filename)
		{
			consoleWrite("Saving database...");
			string content = this.ToJson<Books>();
			await Task.Run(() =>
			{
				File.WriteAllText(filename, content);
			});
			consoleWrite("Database saved");
		}

	}
}
