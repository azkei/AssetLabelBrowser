#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetLabelUtil
{
	public class AssetLabelDialog : EditorWindow
	{
		[MenuItem("Assets/azki-soft/AssetLabelDialog")]
		public static void ShowWindow()
		{
			var window = CreateInstance<AssetLabelDialog>();
			window.ShowAuxWindow();
			if (Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0)
			{
				window.Init(Selection.activeObject);
			}
		}

		AssetLabelList _assetsLabelList;
		Object _selectObject;

		List<int> _groupIndexList = new List<int>();

		public void Init(Object selectObject)
		{
			_assetsLabelList = AssetLabelList.Load();
			_selectObject = selectObject;
			
			string[] labels = AssetDatabase.GetLabels(_selectObject);
			_groupIndexList.Clear();
			foreach (var label in labels)
			{
				_groupIndexList.Add(_assetsLabelList.LabelList.FindIndex(x => x == label));
			}
		}

		private void OnGUI()
		{
			if (_selectObject == null)
			{
				GUILayout.TextArea("ファイル・フォルダが選択されていません。");
				return;
			}

			EditorGUILayout.ObjectField("選択オブジェクト", _selectObject, typeof(Object), false);
			GUILayout.Box("", GUILayout.Height(2), GUILayout.ExpandWidth(true));
			
			using (new GUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField("ラベル:");
				if (GUILayout.Button("追加", GUILayout.Width(60), GUILayout.Height(20)))
				{
					_groupIndexList.Add(0);
				}
			}

			int removeIdx = -1;
			using (new GUILayout.VerticalScope(EditorStyles.helpBox))
			{
				for (int i =0; i < _groupIndexList.Count; i++)
				{
					using (new GUILayout.HorizontalScope())
					{
						_groupIndexList[i] = EditorGUILayout.Popup(_groupIndexList[i], _assetsLabelList.LabelList.ToArray());
						if (GUILayout.Button("削除", GUILayout.Width(60), GUILayout.Height(20)))
						{
							removeIdx = i;
						}
					}
				}
			}

			// ラベルの削除
			if (removeIdx != -1)
			{
				_groupIndexList.RemoveAt(removeIdx);
			}
			
			// ラベル変更の適用
			if (GUILayout.Button("設定を反映", GUILayout.Width(120), GUILayout.Height(20)))
			{
				ApplyLabel();
			}
		}

		void ApplyLabel()
		{
			// ラベルが0なら全部削除、それ以外の場合はラベルを再設定
			if (!_groupIndexList.Any())
			{
				AssetDatabase.ClearLabels(_selectObject);
			}
			else
			{
				List<string> labelList = new List<string>();
				_groupIndexList = _groupIndexList.Distinct().ToList();
				for (int i =0; i < _groupIndexList.Count; i++)
				{
					int groupIdx = _groupIndexList[i];
					labelList.Add(_assetsLabelList.LabelList[groupIdx]);
					AssetDatabase.SetLabels(_selectObject ,labelList.ToArray());
				}
			}
		}

		void OnSelectionChange()
		{
			if (Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0)
			{
				Init(Selection.activeObject);
				Repaint();
			}
		}
	}
}
#endif