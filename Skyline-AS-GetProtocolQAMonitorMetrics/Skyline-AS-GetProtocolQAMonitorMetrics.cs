using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Core.DataMinerSystem.Automation;
using Skyline.DataMiner.Net.Apps.UserDefinableApis;
using Skyline.DataMiner.Net.Apps.UserDefinableApis.Actions;

namespace SkylineASGetProtocolQAMonitorMetrics
{
	public enum QualityMonitorTables
	{
		ProtocolVersionsTable = 1000,
	}

	/// <summary>
	/// Represents a DataMiner user-defined API.
	/// </summary>
	public class Script
	{
		/// <summary>
		/// The API trigger.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		/// <param name="requestData">Holds the API request data.</param>
		/// <returns>An object with the script API output data.</returns>
		[AutomationEntryPoint(AutomationEntryPointType.Types.OnApiTrigger)]
		public ApiTriggerOutput OnApiTrigger(IEngine iEngine, ApiTriggerInput requestData)
		{
			var method = requestData.RequestMethod;
			var route = requestData.Route;
			var body = requestData.RawBody;

			Engine engine = (Engine)iEngine;

			List<ProtocolVersion> protocolVersions = GetProtocolVersions(engine);
			string results = JsonConvert.SerializeObject(protocolVersions);

			return new ApiTriggerOutput
			{
				ResponseBody = results,
				ResponseCode = (int)StatusCode.Ok,
			};
		}

		public List<ProtocolVersion> GetProtocolVersions(Engine engine)
		{
			IDictionary<string, object[]> protocolVersionRows = engine
			.GetDms()
			.GetElement("Quality Monitor")
			.GetTable((int)QualityMonitorTables.ProtocolVersionsTable)
			.GetData();

			List<ProtocolVersion> protocolVersions = protocolVersionRows.GetColumns(
			new uint[] { 0, 1, 2, 3, 4, 5, 6, 23, 19, 22, 13, 21, 14, 25 },
			values => new ProtocolVersion
			 {
				 ID = Convert.ToString(values[0]),
				 Name = Convert.ToString(values[1]),
				 Version = Convert.ToString(values[2]),
				 Branch = Convert.ToString(values[3]),
				 Vendor = Convert.ToString(values[4]),
				 Tagger = Convert.ToString(values[5]),
				 ReleaseDate = DateTime.FromOADate(Convert.ToDouble(values[6])),
				 BasedOnVersion = Convert.ToString(values[7]),
				 QualityScore = Convert.ToDouble(values[8], System.Globalization.CultureInfo.InvariantCulture),
				 QualityScoreDelta = Convert.ToDouble(values[9], System.Globalization.CultureInfo.InvariantCulture),
				 UnitTests = Convert.ToDouble(values[10], System.Globalization.CultureInfo.InvariantCulture),
				 UnitTestsDelta = Convert.ToDouble(values[11], System.Globalization.CultureInfo.InvariantCulture),
				 UnitTestsCoverage = Convert.ToDouble(values[12]),
				 TaskId = Convert.ToString(values[13]), // temporary, will be replaced
			 })
			.SelectMany(pv =>
				Convert.ToString(/* raw task ids string */ pv.TaskId)
					.Split(';')
					.Where(id => !string.IsNullOrWhiteSpace(id))
					.Select(taskId => new ProtocolVersion
					{
						ID = pv.ID,
						Name = pv.Name,
						Version = pv.Version,
						Branch = pv.Branch,
						Vendor = pv.Vendor,
						Tagger = pv.Tagger,
						ReleaseDate = pv.ReleaseDate,
						BasedOnVersion = pv.BasedOnVersion,
						QualityScore = pv.QualityScore,
						QualityScoreDelta = pv.QualityScoreDelta,
						UnitTests = pv.UnitTests,
						UnitTestsDelta = pv.UnitTestsDelta,
						UnitTestsCoverage = pv.UnitTestsCoverage,
						TaskId = taskId.Trim(),
					}))
			.Where(pv => pv.ReleaseDate.Year >= 2026)
			.ToList();

			return protocolVersions;
		}
	}

	public static class DmsExtensions
	{
		public static List<T> GetColumns<T>(this IDictionary<string, object[]> tableData, uint[] columnIndices, Func<object[], T> mapper)
		{
			if (tableData == null || !tableData.Any())
				return new List<T>();

			var rows = tableData.Values.ToArray();
			int rowCount = rows.Length;
			var result = new List<T>(rowCount);

			for (int i = 0; i < rowCount; i++)
			{
				var row = rows[i];
				var selectedValues = columnIndices
					.Select(colIdx => row.Length > colIdx ? row[colIdx] : null)
					.ToArray();

				result.Add(mapper(selectedValues));
			}

			return result;
		}
	}

	public class ProtocolVersion
	{
		public string ID { get; set; }

		public string Name { get; set; }

		public string Version { get; set; }

		public string Branch { get; set; }

		public string BasedOnVersion { get; set; }

		public string Vendor { get; set; }

		public string Tagger { get; set; }

		public string TaskId { get; set; }

		public DateTime ReleaseDate { get; set; }

		public double QualityScore { get; set; }

		public double QualityScoreDelta { get; set; }

		public double UnitTests { get; set; }

		public double UnitTestsDelta { get; set; }

		public double UnitTestsCoverage { get; set; }
	}
}