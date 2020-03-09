using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class SearchForCoordinates : MonoBehaviour
{
	public TextAsset report;
	public TextAsset listForReport;
	public TextAsset preparedList;
	private List<string> readyListFromReport = new List<string>();
	private List<string> recordsForGoogleEarth = new List<string>();

	private const string startKML = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
	private const string startKML1 = "<kml xmlns = \"http://www.opengis.net/kml/2.2\" xmlns:gx=\"http://www.google.com/kml/ext/2.2\" xmlns:kml=\"http://www.opengis.net/kml/2.2\" xmlns:atom=\"http://www.w3.org/2005/Atom\">";
	private const string documentStart = "<Document>";

	private const string documentName = "YOUR KML NAME";

	private const string documentEnd = "</Document>";
	private const string placeMarkStart = "<Placemark>";
	private const string placeMarkEnd = "</Placemark>";
	private const string nameStart = "<name>";
	private const string nameEnd = "</name>";
	private const string coordsStart = "<coordinates>";
	private const string coordsEnd = "</coordinates>";
	private const string kmlEnd = "</kml>";
	private const string pointStart = "<Point>";
	private const string pointEnd = "</Point>";

	private const string pathToResources = "Assets/Resources/";
	private const string outputFolderName = "/Output/";
	private const string listName = "list.txt";
	private const string reportName = "report.txt";
	private const string listForReportName = "listForReport.txt";
	private const string preparedListName = "preparedList.txt";
	private const string namesWithCoords = "namesWithCoordinates.txt";
	private const string newLineStringWithReturn = "\r\n";
	private const string newLineString = "\n";
	private const string allParagraphEnds = "\n|\r|\r\n";
	private const char spaceChar = ' ';
	private string dataPath = Application.dataPath;

	void Start()
	{
		if (report == null)
		{
			report = (TextAsset)AssetDatabase.LoadAssetAtPath(pathToResources + reportName, typeof(TextAsset));
		}
		if (listForReport == null)
		{
			listForReport = (TextAsset)AssetDatabase.LoadAssetAtPath(pathToResources + listForReportName, typeof(TextAsset));
		}
		if (preparedList == null)
		{
			preparedList = (TextAsset)AssetDatabase.LoadAssetAtPath(pathToResources + preparedListName, typeof(TextAsset));
		}

		/***** Uncomment those for certain actions to process on Start: *****/
		/***** (You can also use context menu) *****/
		PrepareList();
		CleanUpPreparedList();
		PrepareKML();
	}

	private char[] splitBy = new char[] { '|' };

	[ContextMenu("Prepare List")]
	private void PrepareList()
	{
		string[] names = listForReport.text.Split(splitBy, StringSplitOptions.RemoveEmptyEntries);
		string[] reportRows = Regex.Split(report.text, newLineStringWithReturn);
		foreach (var row in reportRows)
		{
			foreach (var n in names)
			{
				if (row.StartsWith(n))
				{
					readyListFromReport.Add(row);
				}
			}
		}
		string joinedString = string.Join(newLineString, readyListFromReport);
		File.WriteAllText(dataPath + preparedListName, joinedString);
	}

	[ContextMenu("Clean Up List")]
	private void CleanUpPreparedList()
	{
		string[] rows = Regex.Split(preparedList.text, newLineString);
		List<string> newReportList = new List<string>();
		foreach (var row in rows)
		{
			//if (Regex.IsMatch(row, "[A-Za-z]") == false)
			if (row.Any(char.IsDigit) && !row.Contains(": see"))
			{
				if (Regex.IsMatch(row, "[0]{1}[A-Za-z]"))
				{
					Debug.Log(string.Format("<color=green><b>{0}</b></color>", "found " + row));
					int zeroIndex = row.IndexOf('0');
					char[] charr = row.ToCharArray();
					charr[zeroIndex] = 'ø';
					if (charr.Contains('°'))
					{
						newReportList.Add(new string(charr));
					}
				}
				else if (row.Contains("°"))
				{
					newReportList.Add(row);
				}
			}
			else
			{
				Debug.Log(string.Format("<color=red><b>{0}</b></color>", "excluding " + row));
			}
		}
		string joinedString = string.Join(newLineString, newReportList);
		File.WriteAllText(dataPath + outputFolderName + "cleanedUpList.txt", joinedString);
	}

	[ContextMenu("Prepare KML")]
	public void PrepareKML()
	{
		var reportLines = Regex.Split(preparedList.text, allParagraphEnds);

		foreach (var line in reportLines)
		{
			if (preparedList.text.StartsWith(line))
			{
				continue;
			}
			if (line != string.Empty)
			{
				var split = Regex.Split(line, " |,");
				string placeName = string.Empty;
				string south = string.Empty;
				string eastWest = string.Empty;
				for (int i = 0; i < split.Length; i++)
				{
					if (Regex.IsMatch(split[i], "[0-9](.*)"))
					{
						if (split[i] != "")
						{
							Debug.Log(string.Format("<color=green><b>{0}</b></color>", "now working on: " + split[i]));
							if (split[i].Contains("S"))
							{
								string[] southNumbers = Regex.Split(split[i], "°|'S");
								if (int.Parse(southNumbers[1]) / 6 < 1)
								{
									south += "-" + southNumbers[0] + "." + "0" + (int.Parse(southNumbers[1]) / 0.6f).ToString("#");
								}
								else
								{
									south += "-" + southNumbers[0] + "." + (int.Parse(southNumbers[1]) / 0.6f).ToString("#");
								}
							}
							else if (split[i].Contains("E"))
							{
								string[] eastNumbers = Regex.Split(split[i], "°|'E");
								if (int.Parse(eastNumbers[1]) / 6 < 1)
								{
									eastWest += eastNumbers[0] + "." + "0" + (int.Parse(eastNumbers[1]) / 0.6f).ToString("#");
								}
								else
								{
									eastWest += eastNumbers[0] + "." + (int.Parse(eastNumbers[1]) / 0.6f).ToString("#");
								}
							}
							else if (split[i].Contains("W"))
							{
								string[] westNumbers = Regex.Split(split[i], "°|'W");
								if (int.Parse(westNumbers[1]) / 6 < 1)
								{
									eastWest += "-" + westNumbers[0] + "." + "0" + (int.Parse(westNumbers[1]) / 0.6f).ToString("#");
								}
								else
								{
									eastWest += "-" + westNumbers[0] + "." + (int.Parse(westNumbers[1]) / 0.6f).ToString("#");
								}
							}
						}
					}
					else
					{
						placeName += spaceChar + split[i];
					}
				}


				string combined = string.Empty;
				combined += newLineString + placeMarkStart;
				combined += newLineString + nameStart;
				combined += newLineString + placeName + newLineString + nameEnd;
				combined += newLineString + pointStart + newLineString + coordsStart;
				combined += eastWest + "," + south + ",0";
				combined += coordsEnd + newLineString + pointEnd;
				combined += newLineString + placeMarkEnd;

				recordsForGoogleEarth.Add(combined);
			}
		}

		string joinedString = string.Join(newLineString, startKML, startKML1);
		joinedString += newLineString + documentStart;
		joinedString += newLineString + nameStart;
		joinedString += newLineString + documentName;
		joinedString += newLineString + nameEnd;
		joinedString += string.Join(newLineString, recordsForGoogleEarth.ToArray());
		joinedString += newLineString + documentEnd;
		joinedString += newLineString + kmlEnd;

		File.WriteAllText(dataPath + namesWithCoords, joinedString);
	}
}
