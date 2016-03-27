using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace gITEBooksGrabber
{
	[DataContract]
	public class Book
	{
		[DataMember]
		public string ID;
		[DataMember]
		public string URL;
		[DataMember]
		public string Title;
		[DataMember]
		public string Description;
		[DataMember]
		public string Publisher;
		[DataMember]
		public string Author;
		[DataMember]
		public string ISBN;
		[DataMember]
		public string DatePublished;
		[DataMember]
		public string NumberOfPages;
		[DataMember]
		public string Language;
		[DataMember]
		public string Format;
		[DataMember]
		public string DownloadLink;
		[DataMember]
		public string FileName;
		[DataMember]
		public string SavePath;
		[DataMember]
		public bool Downloaded;
	}
}
