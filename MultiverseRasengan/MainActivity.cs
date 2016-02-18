using Android.App;
using Android.Widget;
using Android.OS;
using System.Net;
using System.IO;
using System;
using System.Threading;
using System.Text;
using HtmlAgilityPack;
using System.Linq;
using Android.Views;
using Android.Graphics;
using Java.Lang;
using System.Text.RegularExpressions;
using Android.Content.PM;

namespace MultiverseRasengan
{
	[Activity (Label = "Multiverse Rasengan", MainLauncher = true, Icon = "@mipmap/icon", ScreenOrientation = ScreenOrientation.Portrait)]

	public class MainActivity : Activity
	{
		System.Timers.Timer MyTimer = new System.Timers.Timer();
		CookieContainer MyCookies = new CookieContainer();
		string Refresh = null;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			SetContentView(Resource.Layout.Login);

			MyTimer.Elapsed += new System.Timers.ElapsedEventHandler(Refresher);
			MyTimer.Interval = 60000;

			Button MyButt = FindViewById<Button>(Resource.Id.BLogin);
			MyButt.Click += delegate 
			{ ThreadPool.QueueUserWorkItem(o => Login(FindViewById<TextView>(Resource.Id.userentry).Text, FindViewById<TextView>(Resource.Id.passentry).Text)); };
		}

		protected override void OnPause()
		{
			ThreadPool.QueueUserWorkItem(o => Logout());
			MyTimer.Stop();
			base.OnPause();
		}

		protected override void OnStop()
		{
			ThreadPool.QueueUserWorkItem(o => Logout());
			MyTimer.Stop();
			base.OnStop();
		}

		protected override void OnDestroy()
		{
			ThreadPool.QueueUserWorkItem(o => Logout());
			MyTimer.Stop();
			base.OnDestroy();
		}

		private void Refresher(object ignoreme, System.Timers.ElapsedEventArgs ignoremetoo)
		{
			if (Refresh == null)
				ThreadPool.QueueUserWorkItem (o => CheckMail());
			else 
			{
				string url = "http://extremelot.leonardo.it/proc/posta/leggilaposta.asp";
				HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
				request.CookieContainer = MyCookies;
				request.GetResponse().Close();
			}
		}

		private void Login(string Nick, string Pass)
		{
			Loading("Sto entrando a Lot..");

			if (Nick == "" || Pass == "") 
				PostLogout ("Errore: riempi tutti i campi!");
			else
				try
				{				
					string url = "http://extremelot.leonardo.it/proc/login_nuovo.asp";
					HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
					request.CookieContainer = MyCookies;

					// Set the Method property of the request to POST.
					request.Method = "POST";
					// Create POST data and convert it to a byte array.
					string postData = "id="+Nick+"&pass="+Pass;
					byte[] byteArray = Encoding.UTF8.GetBytes(postData);
					// Set the ContentType property of the WebRequest.
					request.ContentType = "application/x-www-form-urlencoded";
					// Set the ContentLength property of the WebRequest.
					request.ContentLength = byteArray.Length;
					// Get the request stream.
					Stream dataStream = request.GetRequestStream();
					// Write the data to the request stream.
					dataStream.Write(byteArray, 0, byteArray.Length);
					// Close the Stream object.
					dataStream.Close();

					HttpWebResponse myResp = (HttpWebResponse) request.GetResponse();
					StreamReader reader = new StreamReader(myResp.GetResponseStream());
					string responseText = reader.ReadToEnd();

					if (responseText.IndexOf ("Bentornat") != -1) 
					{
						MyTimer.Start();
						ThreadPool.QueueUserWorkItem(o => CheckMail());
					}
					else if (responseText.IndexOf("sessione") != -1)
						PostLogout("Login fallito: giá collegato!");
					else if (responseText.IndexOf("corrispondono") != -1)
						PostLogout("Login fallito: dati errati!");
					else
						PostLogout(responseText);
				}
				catch (System.Exception) 
				{ PostLogout ("Errore: Sei collegato a Internet ?"); }
		}

		private void Menu()
		{
			Loading("");

			RunOnUiThread (() =>
			{ 
				SetContentView(Resource.Layout.Menu);

				Button BMail = FindViewById<Button>(Resource.Id.Mail);
				BMail.Click += delegate 
				{ ThreadPool.QueueUserWorkItem(o => CheckMail()); };

				Button BGC = FindViewById<Button>(Resource.Id.GlobalChat);
				BGC.Click += delegate 
				{  };

				Button BExit = FindViewById<Button>(Resource.Id.Exit);
				BExit.Click += delegate 
				{ ThreadPool.QueueUserWorkItem(o => Logout()); };
			});
		}

		private void CheckMail()
		{
			Loading("Sto controllando la posta..");

			string url = "http://extremelot.leonardo.it/proc/posta/leggilaposta.asp";
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
			request.CookieContainer = MyCookies;

			HttpWebResponse myResp = (HttpWebResponse) request.GetResponse();
			string charSet = myResp.CharacterSet;
			Encoding encoding = System.String.IsNullOrEmpty(charSet)
				? Encoding.Default
				: Encoding.GetEncoding(charSet);
			StreamReader reader = new StreamReader(myResp.GetResponseStream(), encoding);
			string responseText = reader.ReadToEnd();

			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(responseText);
			HtmlNode node = doc.DocumentNode;
			Array TRs = node.Descendants("tr").ToArray();

			ThreadPool.QueueUserWorkItem(o => RenderMail(TRs));
		}

		private void RenderMail(Array TRs)
		{
			Loading("");
			Refresh = "Mail";

			RunOnUiThread (() =>
			{ 
				SetContentView(Resource.Layout.Mail);
				LinearLayout Main = FindViewById<LinearLayout>(Resource.Id.MyMainLayout); 				
				RelativeLayout RemoveMe = FindViewById<RelativeLayout>(Resource.Id.RemoveMe);
				Main.RemoveView(RemoveMe);

				Button BLogout = FindViewById<Button>(Resource.Id.Back);
				BLogout.Click += delegate 
				{ ThreadPool.QueueUserWorkItem(o => Logout()); };

				Button BCompose = FindViewById<Button>(Resource.Id.Compose);
				BCompose.Click += delegate 
				{ ThreadPool.QueueUserWorkItem(o => Compose()); };

				int CNT = 0;
				RelativeLayout.LayoutParams MyPars;		
				foreach (HtmlNode TR in TRs)
				{
					CNT++;
					if (CNT <= 2 || CNT == TRs.Length)
						continue;
					
					string HTML = TR.InnerHtml;
					HtmlDocument doc = new HtmlDocument();
					doc.LoadHtml(HTML);
					HtmlNode node = doc.DocumentNode;

					HtmlNode a = node.Descendants("a").Last();
					string ID =	a.Attributes.First().Value;
					ID = ID.Split('=')[1];
					string From = a.InnerText;

					HtmlNode Temp; 
					Array Fonts = node.Descendants("font").ToArray();
					Temp = (HtmlNode) Fonts.GetValue(2);
					string Time = Temp.InnerText;
					Temp = (HtmlNode) Fonts.GetValue(3);
					string Where = Temp.InnerText;
					Temp = (HtmlNode) Fonts.GetValue(4);
					string MSG = Regex.Split(Temp.InnerText, "\\\n")[0];

					RelativeLayout NewLayout = new RelativeLayout(this);
					if (CNT % 2 == 0)
						NewLayout.SetBackgroundColor(Color.ParseColor("#F0E68C"));
					else
						NewLayout.SetBackgroundColor(Color.ParseColor("#D1CD76"));

					MyPars = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent);
					MyPars.AddRule(LayoutRules.AlignParentLeft);
					MyPars.AddRule(LayoutRules.AlignParentTop);
					TextView Mittente = new TextView(this)
					{ Text = From };
					Mittente.Id = 6901;
					Mittente.LayoutParameters = MyPars;
					Mittente.SetTextColor(Color.Black);
					NewLayout.AddView(Mittente);

					MyPars = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent);
					MyPars.AddRule(LayoutRules.AlignParentRight);
					MyPars.AddRule(LayoutRules.AlignParentTop);
					TextView Locazione = new TextView(this)
					{ Text = Where };
					Locazione.Id = 6902;
					Locazione.LayoutParameters = MyPars;
					Locazione.SetTypeface(Locazione.Typeface, TypefaceStyle.Italic);
					Locazione.SetTextColor(Color.Black);
					NewLayout.AddView(Locazione);

					MyPars = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent);
					MyPars.AddRule(LayoutRules.CenterHorizontal);
					MyPars.AddRule(LayoutRules.Below, 6901);
					TextView Testo = new TextView(this)
					{ Text = MSG };
					Testo.Id = 6903;
					Testo.LayoutParameters = MyPars;
					Testo.SetTypeface(Testo.Typeface, TypefaceStyle.Bold);
					Testo.SetTextColor(Color.Black);
					NewLayout.AddView(Testo);

					MyPars = new RelativeLayout.LayoutParams(150, 45);
					MyPars.AddRule(LayoutRules.AlignParentLeft);
					MyPars.AddRule(LayoutRules.Below, 6903);
					Button Leggi = new Button(this)
					{ Text = "Leggi" };
					Leggi.Id = 6904;
					Leggi.LayoutParameters = MyPars;
					Leggi.SetBackgroundResource(Resource.Drawable.LotButton);
					Leggi.SetTextColor(Color.White);
					Leggi.SetTextSize(Android.Util.ComplexUnitType.Dip, 15);
					Leggi.Click += delegate 
					{ ThreadPool.QueueUserWorkItem(o => ReadMail(ID)); };
					NewLayout.AddView(Leggi);

					MyPars = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent);
					MyPars.AddRule(LayoutRules.CenterHorizontal);
					MyPars.AddRule(LayoutRules.Below, 6903);
					MyPars.SetMargins(0, 5, 0, 0);
					TextView Orario = new TextView(this)
					{ Text = Time };
					Orario.Id = 6905;
					Orario.LayoutParameters = MyPars;
					Orario.SetTextColor(Color.Black);
					NewLayout.AddView(Orario);

					MyPars = new RelativeLayout.LayoutParams(150, 45);
					MyPars.AddRule(LayoutRules.AlignParentRight);
					MyPars.AddRule(LayoutRules.Below, 6903);
					Button Cancella = new Button(this)
					{ Text = "Elimina" };
					Cancella.Id = 6906;
					Cancella.LayoutParameters = MyPars;
					Cancella.SetBackgroundResource(Resource.Drawable.LotButton);
					Cancella.SetTextColor(Color.White);
					Cancella.SetTextSize(Android.Util.ComplexUnitType.Dip, 15);
					Cancella.Click += delegate 
					{ ThreadPool.QueueUserWorkItem(o => DeleteMail(ID)); };
					NewLayout.AddView(Cancella);

					Main.AddView(NewLayout);
				}
			});
		}

		private void DeleteMail(string ID)
		{
			Loading("Sto cancellando la missiva..");

			string url = "http://extremelot.leonardo.it/proc/posta/cancellaposta.asp?ID="+ID;
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
			request.CookieContainer = MyCookies;
			request.GetResponse().Close();

			ThreadPool.QueueUserWorkItem(o => CheckMail());
		}

		private void ReadMail(string ID)
		{
			Loading("Sto aprendo la missiva..");

			string url = "http://extremelot.leonardo.it/proc/posta/apriposta.asp?msg="+ID;
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
			request.CookieContainer = MyCookies;

			HttpWebResponse myResp = (HttpWebResponse) request.GetResponse();
			string charSet = myResp.CharacterSet;
			Encoding encoding = System.String.IsNullOrEmpty(charSet)
				? Encoding.Default
				: Encoding.GetEncoding(charSet);
			StreamReader reader = new StreamReader(myResp.GetResponseStream(), encoding);
			string responseText = reader.ReadToEnd();

			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(responseText);
			HtmlNode node = doc.DocumentNode;

			HtmlNode Mail = node.Descendants("tr").First();
			Array Testi = Mail.Descendants("font").ToArray();
			string Data = ((HtmlNode) Testi.GetValue(0)).InnerText;

			HtmlNode MixTesti = ((HtmlNode) Testi.GetValue(1));
			string Testo, Vecchio;
			try
			{
				Testo = Regex.Split(MixTesti.InnerHtml, "<p>")[0];
				Vecchio = MixTesti.Descendants("i").First().InnerHtml;
			}
			catch (System.Exception) 
			{
				Testo = MixTesti.InnerHtml;
				Vecchio = "";
			}

			string Mittente = ((HtmlNode) Testi.GetValue(Testi.Length-1)).InnerText;
			Testo = Testo.Replace ("<br>", JavaSystem.GetProperty ("line.separator"));
			Vecchio = Vecchio.Replace ("<br>", JavaSystem.GetProperty ("line.separator"));

			Array As = node.Descendants("a").ToArray();
			string Prev = ""; string Next = "";
			foreach (HtmlNode A in As) 
			{
				string Attr = A.Attributes[0].Value;
				if (Attr.IndexOf("Apriposta") != -1)
				{
					if (A.InnerHtml.IndexOf("sx") != -1)
						Prev = Attr.Split('=')[1];
					else
						Next = Attr.Split('=')[1];
				}
			}

			ThreadPool.QueueUserWorkItem(o => ReplyMail(ID, Prev, Next, Mittente, Data, Testo, Vecchio));
		}

		private void ReplyMail(string ID, string Prev, string Next, string Mittente, string Data, string Testo, string Vecchio)
		{
			Loading("");

			string NewLine = JavaSystem.GetProperty ("line.separator");
			string Bar = "===========================";

			RunOnUiThread (() =>
			{ 
				SetContentView(Resource.Layout.ReplyMail);
				Button List = FindViewById<Button>(Resource.Id.MailList);
				Button Delete = FindViewById<Button>(Resource.Id.MailDelete);
				Button BPrev = FindViewById<Button>(Resource.Id.MailPrev);
				Button BNext = FindViewById<Button>(Resource.Id.MailNext);
				Button BMailSend = FindViewById<Button>(Resource.Id.MailSend);
				TextView MailText = FindViewById<TextView>(Resource.Id.MailText);
				
				if (Prev == "")
					BPrev.Enabled = false;
				else
					BPrev.Click += delegate 
					{ ThreadPool.QueueUserWorkItem(o => ReadMail(Prev)); };						

				if (Next == "")
					BNext.Enabled = false;
				else
					BNext.Click += delegate 
					{ ThreadPool.QueueUserWorkItem(o => ReadMail(Next)); };						

				BMailSend.Click += delegate 
				{ ThreadPool.QueueUserWorkItem(o => SendReply(ID, Mittente, FindViewById<EditText>(Resource.Id.MailReply).Text)); };				

				List.Click += delegate 
				{ ThreadPool.QueueUserWorkItem(o => CheckMail()); };

				Delete.Click += delegate 
				{ ThreadPool.QueueUserWorkItem(o => DeleteMail(ID)); };

				string ToShow = Mittente + " - " + Data + NewLine 
								+ Bar + NewLine 
								+ NewLine
								+ Testo;

				if (Vecchio != "")
					ToShow = ToShow + NewLine 
					+ NewLine
					+ Bar + NewLine 
					+ NewLine 
					+ "Avevate scritto:" + NewLine 
					+ Vecchio;
							
				MailText.Text = ToShow;
			});
		}

		private void SendReply(string ID, string Destinatario, string Testo)
		{
			Loading("Sto inviando la missiva..");

			Testo = Testo.Replace("\n", "\r\n");
			string url = "http://extremelot.leonardo.it/proc/posta/mandaposta.asp?Destinatario="+Destinatario+"&msg="+ID;
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
			request.CookieContainer = MyCookies;

			// Set the Method property of the request to POST.
			request.Method = "POST";
			// Create POST data and convert it to a byte array.
			string postData = "Testo="+Uri.EscapeDataString(Testo)+"&Ambito=GIOCO&Faccina=0&inoltro=0";
			byte[] byteArray = Encoding.UTF8.GetBytes(postData);
			// Set the ContentType property of the WebRequest.
			request.ContentType = "application/x-www-form-urlencoded";
			// Set the ContentLength property of the WebRequest.
			request.ContentLength = byteArray.Length;
			// Get the request stream.
			Stream dataStream = request.GetRequestStream();
			// Write the data to the request stream.
			dataStream.Write(byteArray, 0, byteArray.Length);
			// Close the Stream object.
			dataStream.Close();
			request.GetResponse().Close();

			ThreadPool.QueueUserWorkItem(o => CheckMail());
		}

		private void Compose()
		{
			Loading("");

			RunOnUiThread (() =>
			{ 
				SetContentView(Resource.Layout.ComposeMail);

				Button BList = FindViewById<Button>(Resource.Id.MailList);
				Button BMailSend = FindViewById<Button>(Resource.Id.MailSend);

				BMailSend.Click += delegate 
				{ ThreadPool.QueueUserWorkItem(o => SendMail(FindViewById<EditText>(Resource.Id.Nickname).Text, FindViewById<EditText>(Resource.Id.MailText).Text)); };				

				BList.Click += delegate 
				{ ThreadPool.QueueUserWorkItem(o => CheckMail()); };
			});			
		}

		private void SendMail(string Destinatario, string Testo)
		{
			Loading("Sto inviando la missiva..");

			Testo = Testo.Replace("\n", "\r\n");
			string url = "http://extremelot.leonardo.it/proc/posta/mandaltri.asp";
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
			request.CookieContainer = MyCookies;

			// Set the Method property of the request to POST.
			request.Method = "POST";
			// Create POST data and convert it to a byte array.
			string postData = "Testo="+Uri.EscapeDataString(Testo)+"&nome="+Destinatario+"&Ambito=GIOCO&Faccina=0&inoltro=0";
			byte[] byteArray = Encoding.UTF8.GetBytes(postData);
			// Set the ContentType property of the WebRequest.
			request.ContentType = "application/x-www-form-urlencoded";
			// Set the ContentLength property of the WebRequest.
			request.ContentLength = byteArray.Length;
			// Get the request stream.
			Stream dataStream = request.GetRequestStream();
			// Write the data to the request stream.
			dataStream.Write(byteArray, 0, byteArray.Length);
			// Close the Stream object.
			dataStream.Close();
			request.GetResponse().Close();

			ThreadPool.QueueUserWorkItem(o => CheckMail());
		}

		private void Logout()
		{
			Loading("Sto uscendo da Lot..");

			string url = "http://extremelot.leonardo.it/lotnew/close.asp";
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
			request.CookieContainer = MyCookies;
			request.GetResponse().Close();

			PostLogout("Logout effettuato!");
		}

		private void Loading(string MSG)
		{
			Refresh = null;

			RunOnUiThread (() =>
			{ 
				SetContentView(Resource.Layout.Loading);
				FindViewById<TextView>(Resource.Id.MyText).Text = MSG;
			});			
		}

		private void PostLogout(string MSG)
		{
			MyTimer.Stop();

			RunOnUiThread (() =>
			{ 
				SetContentView(Resource.Layout.Login);
				FindViewById<TextView>(Resource.Id.MyMSG).Text = MSG;

				Button MyButt = FindViewById<Button>(Resource.Id.BLogin);
				MyButt.Click += delegate 
				{ ThreadPool.QueueUserWorkItem(o => Login(FindViewById<TextView>(Resource.Id.userentry).Text, FindViewById<TextView>(Resource.Id.passentry).Text)); };
			});
		}
	}
}


