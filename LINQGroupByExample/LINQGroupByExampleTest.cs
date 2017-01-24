using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace LINQGroupByExample
{
	// Demonstrates GROUP BY and JOIN in LINQ
	[TestClass]
	public class LINQGroupByExampleTest
	{
		// ENTITY: Represents a Web Page Url
		class WebPage
		{
			public int Id { get; set; }
			public string Url { get; set; }
		}

		// ENTITY: Records an access of a Web Page by Url 
		class WebPageAccess
		{
			public int Id { get; set; }
			public string Url { get; set; }
			public DateTime AccessedAt { get; set; }
		}

		// ENTITY: Records latency experienced in accessing a
		//	Web Page represented by Url
		class WebPageLatency
		{
			public int Id { get; set; }
			public string Url { get; set; }
			public int LoadTimeMs { get; set; }
		}

		// Test Data - These could be Entity Framework DbSets
		List<WebPage> WebPages = new List<WebPage>();
		List<WebPageAccess> WebPageAccesses = new List<WebPageAccess>();
		List<WebPageLatency> WebPageLatencies = new List<WebPageLatency>();

		// Test Urls
		const string URL1 = "abc.com";
		const string URL2 = "def.com";
		const string URL3 = "xyz.com";

		// Populate data for testing
		void SeedData()
		{
			// We are tracking three web pages represented
			//	by their respective Urls
			this.WebPages.Add(new WebPage() { Url = URL1 });
			this.WebPages.Add(new WebPage() { Url = URL2 });
			this.WebPages.Add(new WebPage() { Url = URL3 });

			for (int i = 0; i < 10; i++)
			{
				// record 10 accesses for URL1
				this.WebPageAccesses.Add(new WebPageAccess() { Url = URL1 });
				// record 5 accesses for URL2
				if (i % 2 == 0)
				{
					this.WebPageAccesses.Add(new WebPageAccess() { Url = URL2 });
				}

				// record a latency of 2 for URL1
				this.WebPageLatencies.Add(new WebPageLatency() { Url = URL1, LoadTimeMs = 2 });
				// record a latency of 4 for URL2
				if (i % 2 == 0)
				{
					this.WebPageLatencies.Add(new WebPageLatency() { Url = URL2, LoadTimeMs = 4 });
				}
			}
			// no data recorded for URL3
		}

		const int ASSERT_URL1_TOTAL_VIEWS = 10;
		const int ASSERT_URL2_TOTAL_VIEWS = 5;
		const int ASSERT_URL1_AVG_LATENCY_MS = 2;
		const int ASSERT_URL2_AVG_LATENCY_MS = 4;

		[TestMethod]
		public void Test()
		{
			SeedData();

			// Group web page accesses by Url, count accesses by Url
			var webPageAccessGroups = from row in this.WebPageAccesses
										  // we can group by multiple fields, if needed
									  group row by new { row.Url } into urlAccessGroup
									  select new                        // group projection
									  {
										  Url = urlAccessGroup.Key.Url,
										  TotalViews = urlAccessGroup.Count()
									  };

			// Group web page latency by Url, average latency by Url
			var webPageLatencyGroups = from row in this.WebPageLatencies
									   group row by new { row.Url } into urlLatencyGroup
									   select new                       // group projection
									   {
										   Url = urlLatencyGroup.Key.Url,
										   AvgLatencyMs = urlLatencyGroup.Average(g => g.LoadTimeMs)
									   };

			// Join the main table of Urls (WebPages) with their respective
			//	access (view) counts and latencies
			var webPagesStatsByUrl = from webpage in this.WebPages
									 join webPageAccessGroup in webPageAccessGroups on new { Url = webpage.Url }
									 equals new { Url = webPageAccessGroup.Url } into webPageAccesses
									 join webPageLatencyGroup in webPageLatencyGroups on new { Url = webpage.Url }
									 equals new { Url = webPageLatencyGroup.Url } into webPageLatencies
									 from webPageAccess in webPageAccesses.DefaultIfEmpty() // used below for projection
									 from webPageLatency in webPageLatencies.DefaultIfEmpty() // used below for projection
									 select new     // new projection
									 {
										 Url = webpage.Url,
										 TotalViews = webPageAccess == null ? 0 : webPageAccess.TotalViews,
										 AvgLatencyMs = webPageLatency == null ? 0 : webPageLatency.AvgLatencyMs
									 };
			Debug.WriteLine("== Results ==");
			Debug.WriteLine("Url\t\t\tTotalViews\tAvgLatencyMs");
			foreach (var row in webPagesStatsByUrl)
			{
				Debug.WriteLine($"{row.Url}\t\t{row.TotalViews}\t\t\t{row.AvgLatencyMs}");
			}

			Assert.AreEqual(ASSERT_URL1_TOTAL_VIEWS, webPagesStatsByUrl.Where(w => w.Url == URL1).FirstOrDefault().TotalViews);
			Assert.AreEqual(ASSERT_URL2_TOTAL_VIEWS, webPagesStatsByUrl.Where(w => w.Url == URL2).FirstOrDefault().TotalViews);
			Assert.AreEqual(0, webPagesStatsByUrl.Where(w => w.Url == URL3).FirstOrDefault().TotalViews);

			Assert.AreEqual(ASSERT_URL1_AVG_LATENCY_MS, webPagesStatsByUrl.Where(w => w.Url == URL1).FirstOrDefault().AvgLatencyMs);
			Assert.AreEqual(ASSERT_URL2_AVG_LATENCY_MS, webPagesStatsByUrl.Where(w => w.Url == URL2).FirstOrDefault().AvgLatencyMs);
			Assert.AreEqual(0, webPagesStatsByUrl.Where(w => w.Url == URL3).FirstOrDefault().AvgLatencyMs);
		}
	}
}
