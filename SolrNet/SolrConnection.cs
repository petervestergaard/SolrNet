using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using SolrNet.Exceptions;
using SolrNet.Tests;
using SolrNet.Utils;

namespace SolrNet {
	public class SolrConnection : ISolrConnection {
		private string serverURL;
		private IHttpWebRequestFactory httpWebRequestFactory;
		private Encoding enc = Encoding.UTF8;

		public SolrConnection(string serverURL, IHttpWebRequestFactory httpWebRequestFactory) {
			ServerURL = serverURL;
			this.httpWebRequestFactory = httpWebRequestFactory;
		}

		public string ServerURL {
			get { return serverURL; }
			set {
				try {
					Uri u = new Uri(value);
					if (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps)
						throw new InvalidURLException("Only HTTP or HTTPS protocols are supported");
				} catch (ArgumentException e) {
					throw new InvalidURLException(e);
				} catch (UriFormatException e) {
					throw new InvalidURLException(e);
				}
				serverURL = value;
			}
		}

		public Encoding Enc {
			get { return enc; }
			set { enc = value; }
		}

		public static byte[] ReadFully(Stream stream) {
			byte[] buffer = new byte[32768];
			using (MemoryStream ms = new MemoryStream()) {
				while (true) {
					int read = stream.Read(buffer, 0, buffer.Length);
					if (read <= 0)
						return ms.ToArray();
					ms.Write(buffer, 0, read);
				}
			}
		}

		public string Post(string s, string contentType) {
			IHttpWebRequest request = httpWebRequestFactory.Create(serverURL);
			request.Method = HttpWebRequestMethod.POST;
			using (Stream postParams = request.GetRequestStream()) {
				postParams.Write(enc.GetBytes(s), 0, s.Length);
				request.ContentType = contentType;
				request.ContentLength = s.Length;
				using (IHttpWebResponse response = request.GetResponse()) {
					using (Stream rStream = response.GetResponseStream()) {
						return enc.GetString(ReadFully(rStream));
					}
				}
			}
		}

		public XmlDocument PostXml(XmlDocument xml) {
			string xmlResponse = Post(xml.ToString(), "text/xml");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xmlResponse);
			return doc;
		}

		public string Get(string relativeUrl, IDictionary<string, string> parameters) {
			UriBuilder u = new UriBuilder(serverURL);
			u.Path += relativeUrl;
			u.Query = Func.Reduce(
				Func.Map<KeyValuePair<string, string>, string>(parameters,
				                                               delegate(KeyValuePair<string, string> input) {
				                                               	return
				                                               		string.Format("{0}={1}", HttpUtility.UrlEncode(input.Key),
				                                               		              HttpUtility.UrlEncode(input.Value));
				                                               }), "?",
				delegate(string x, string y) {
					return string.Format("{0}&{1}", x, y);
				});
			Console.WriteLine(u.Uri);
			IHttpWebRequest request = httpWebRequestFactory.Create(u.Uri);
			request.Method = HttpWebRequestMethod.GET;
			using (IHttpWebResponse response = request.GetResponse()) {
				using (Stream rStream = response.GetResponseStream()) {
					return enc.GetString(ReadFully(rStream));
				}
			}
		}
	}
}