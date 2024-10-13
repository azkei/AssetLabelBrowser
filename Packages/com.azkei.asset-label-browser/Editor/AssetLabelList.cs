#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace AssetLabelUtil
{
	public class AssetLabelList : ScriptableObject
	{
		private const string CGroupPath = "Packages/com.azkei.asset-label-browser/Editor/AssetLabelListData.asset";

		public static AssetLabelList Load()
		{
			var guids = AssetDatabase.FindAssets("AssetLabelListData");
			string[] paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToArray();
			if (!paths.Contains(CGroupPath))
			{
				AssetLabelList groupData = ScriptableObject.CreateInstance<AssetLabelList>();
				AssetDatabase.CreateAsset(groupData, CGroupPath);
			}

			return AssetDatabase.LoadAssetAtPath<AssetLabelList>(CGroupPath);
		}
		
		public void Save()
		{
			DistinctClear();
			LabelList.Sort();
			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
		}

		[field: FormerlySerializedAs("LabelList")]
		[field: SerializeField]
		public List<string> LabelList { get; set; } = new List<string>();

		void DistinctClear()
		{
			LabelList = LabelList.Distinct().ToList();
		}
	}
}
#endif