using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

// need these includes to perform the request
using System.Net;
using System.IO;
using System.Text;
using System.Xml;
using System.Collections;

/**
 * This program is used to connect to the XML gateway API.
 * Each request can be altered by you to test the API and
 * validate the responses you receive back.
 */
namespace gateway_example
{
	public partial class _Default : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			Random rgen = new Random();

			Response.Write("This will send a get request to the xmlgateway and attempt to perform an auth operation<br><hr><br>"); 

			string lcUrl = "https://secure.1stpaygateway.net/secure/gateway/xmlgateway.aspx";
			
			HttpWebRequest loHttp = (HttpWebRequest)WebRequest.Create(lcUrl);

			// *** Send any POST data 
			string lcPostData =
				@"<?xml version=""1.0"" encoding=""UTF-8""?> 
				<TRANSACTION>
					<FIELDS>
					<FIELD KEY=""transaction_center_id"">1264</FIELD>
					<FIELD KEY=""gateway_id"">a91c38c3-7d7f-4d29-acc7-927b4dca0dbe</FIELD>
					<FIELD KEY=""operation_type"">auth</FIELD>
					<FIELD KEY=""order_id"">ectest " + rgen.Next(0, 1000) + rgen.Next(1000, 5000) + @"</FIELD>
					<FIELD KEY=""total"">1.00</FIELD>
					<FIELD KEY=""card_name"">Visa</FIELD>
					<FIELD KEY=""card_number"">4111111111111111</FIELD>
					<FIELD KEY=""card_exp"">1020</FIELD>
					<FIELD KEY=""cvv2""></FIELD>
					<FIELD KEY=""owner_name"">Bob Tester</FIELD>
					<FIELD KEY=""owner_street"">123 Test Rd</FIELD>
					<FIELD KEY=""owner_city"">Cityville</FIELD>
					<FIELD KEY=""owner_state"">PA</FIELD>
					<FIELD KEY=""owner_zip"">19036</FIELD>
					<FIELD KEY=""owner_country"">US</FIELD>
					<FIELD KEY=""owner_email""></FIELD>
					<FIELD KEY=""owner_phone""></FIELD>
					<FIELD KEY=""recurring"">0</FIELD>
					<FIELD KEY=""recurring_type""></FIELD>
					<FIELD KEY=""remote_ip_address"">" + Request.ServerVariables["REMOTE_ADDR"].ToString() + @"</FIELD>
					</FIELDS> 
				</TRANSACTION>";

			//done for visible printout
			Response.Write("<b>Message Being Sent:</b><br><hr><br>");
			Response.Write("<pre>" + lcPostData.Replace("<", "&lt;").Replace(">", "&gt;") + "</pre>");

			loHttp.Method = "POST";
			byte[] lbPostBuffer = System.Text.Encoding.GetEncoding(1252).GetBytes(lcPostData); 
			loHttp.ContentLength = lbPostBuffer.Length;

			Stream loPostData = loHttp.GetRequestStream(); 
			loPostData.Write(lbPostBuffer, 0, lbPostBuffer.Length); 
			loPostData.Close();

			HttpWebResponse loWebResponse = (HttpWebResponse)loHttp.GetResponse();

			Encoding enc = System.Text.Encoding.GetEncoding(1252);

			StreamReader loResponseStream = new StreamReader(loWebResponse.GetResponseStream(), enc);

			string lcHtml = loResponseStream.ReadToEnd();

			loWebResponse.Close(); 
			loResponseStream.Close();

			//done for visible printout
			Response.Write("<br><hr><br><b>Raw Response:</b><br><hr><br>");
			Response.Write("<pre>" + lcHtml.Replace("<FIELD ", "\n<FIELD ").Replace("<", "&lt;").Replace(">", "&gt;") + "</pre>");

			Response.Write("<br><hr><br><b>Parsed XML Response:</b><br><hr><br>"); 
			Hashtable xml_hash = parseXML(lcHtml);
			if (xml_hash != null && xml_hash.Count > 0)
			{
				foreach (string k in xml_hash.Keys) 
				{
					Response.Write(k + " <=> " + xml_hash[k].ToString() + "<br>"); 
				}
			} 
			else 
			{
				Response.Write("No XML was parsed"); 
			}
		}

		/// <summary>
		/// takes in raw xml string and attempts to parse it into a workable hash.
		/// all valid xml for the gateway contains
		/// <transaction><fields><field key="attribute name">value</field></fields></transaction>
		/// there will be 1 or more (should always be more than 1 to be valid) field tags
		/// this method will take the attribute name and make that the hash key and then the value is the value 
		/// if an error occurs then the error key will be added to the hash.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		private Hashtable parseXML(string xml)
		{
			Hashtable ret_hash = new Hashtable(); //stores key values to return 
			XmlTextReader txtreader = null;
			XmlValidatingReader reader = null;
			
			if (xml != null && xml.Length > 0) 
			{

				try 
				{
					//Implement the readers.
					txtreader = new XmlTextReader(new System.IO.StringReader(xml)); 
					reader = new XmlValidatingReader(txtreader);

					//Parse the XML and display the text content of each of the elements. 
					while (reader.Read())
					{
						if (reader.IsStartElement() && reader.Name.ToLower() == "field") 
						{
							if (reader.HasAttributes) 
							{
								//we want the key attribute value
								ret_hash[reader.GetAttribute(0).ToLower()] = reader.ReadString(); 
							}
							else 
							{
								ret_hash["error"] = "All FIELD tags must contains a KEY attribute."; 
							}
						}
					} //ends while
				}
				catch (Exception e) 
				{
					//handle exceptions
					ret_hash["error"] = e.Message; 
				}
				finally 
				{
					if (reader != null) reader.Close();
				} 
			}
			else 
			{
				//incoming xml is empty
				ret_hash["error"] = "No data was present. Valid XML must be sent in order to process a transaction."; 
			}
		
			return ret_hash; 
		} //ends parseXML
	}
}