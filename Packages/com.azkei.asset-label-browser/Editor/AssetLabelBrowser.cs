using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetLabelUtil
{
	public class AssetLabelBrowser : EditorWindow
	{
		[MenuItem("Tools/azki-soft/AssetLabelBrowser")]
		static void ShowWindow()
		{
			var window = GetWindow<AssetLabelBrowser>();
			window.Initialize();
		}

		AssetLabelList _assetsLabelList;
		Object _selectObject;

		Vector2 _leftScrollPos;
		Vector2 _rightScrollPos;
		string _addLabelName;
		
		List<string> _labelList = new List<string>();
		readonly List<int> _removeLabelIdxList = new List<int>();
		List<bool> _selectLabelToggleList = new List<bool>();
		List<bool> _labelFilterToggleList = new List<bool>();
		
		Dictionary<string, bool> _groupFoldout = new Dictionary<string, bool>();
		Dictionary<string, List<Object>> _labelAssetListDic = new Dictionary<string, List<Object>>();
		Color _baseBackGroundColor;
		int _labelFilterIndex = 0;
		bool _labelFilterOrFind = true;
		bool _isFocus = true;

		void Initialize()
		{
			_baseBackGroundColor = GUI.backgroundColor;
			_assetsLabelList = AssetLabelList.Load();
			_addLabelName = "";
			
			_groupFoldout.Clear();
			_labelList.Clear();
			_removeLabelIdxList.Clear();
			_labelAssetListDic.Clear();
			_selectLabelToggleList.Clear();
			_labelFilterToggleList.Clear();
			
			foreach (var label in _assetsLabelList.LabelList)
			{
				_groupFoldout.TryAdd(label, true);
				_labelList.Add(label);
				_selectLabelToggleList.Add(false);
				_labelFilterToggleList.Add(false);
				var assetPaths = AssetDatabase.FindAssets("l:"+label).Select(AssetDatabase.GUIDToAssetPath);

				if (_labelAssetListDic.ContainsKey(label))
				{
					continue;
				}
				
				_labelAssetListDic.Add(label, new List<Object>());
				foreach (var path in assetPaths)
				{
					var prefab = AssetDatabase.LoadAssetAtPath<Object>(path);
					if (!AssetDatabase.GetLabels(prefab).Any(x => x == label))
					{
						continue;
					}

					_labelAssetListDic[label].Add(prefab);
				}
			}
		}

		void OnValidate()
		{
			Initialize();
		}

		void OnFocus()
		{
			_isFocus = true;
		}

		void OnLostFocus()
		{
			_isFocus = false;
		}

		void OnProjectChange()
		{
			// フォーカスされている時はこのスクリプトがラベルを変更したので初期化しない。
			if(_isFocus) return;

			Initialize();
		}

		void OnSelectionChange()
		{
			if (Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0)
			{
				if (!AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]).StartsWith("Packages/"))
				{
					_selectObject = Selection.activeObject;
				}
				else
				{
					_selectObject = null;
				}
			}
			else
			{
				_selectObject = null;
			}
			Repaint();
		}
		
		void OnGUI()
		{
			HeadDraw();

			using (new GUILayout.HorizontalScope())
			{
				LabelObjectDraw();
				
				GUILayout.Box("", GUILayout.Width(2), GUILayout.ExpandHeight(true));
				
				LabelListDraw();
			}
		}

		void HeadDraw()
		{
			using (new GUILayout.HorizontalScope())
			{
				if (GUILayout.Button("選択オブジェクトのラベルを変更", GUILayout.Width(200), GUILayout.Height(20)))
				{
					if (_selectObject != null)
					{
						Selection.activeObject = _selectObject;
						AssetLabelDialog.ShowWindow();
					}
				}

				EditorGUILayout.ObjectField("", _selectObject, typeof(Object), false);
			}
		}

		void LabelObjectDraw()
		{
			using (new GUILayout.VerticalScope())
			{
				using (new GUILayout.VerticalScope(EditorStyles.helpBox))
				{
					LabelObjectHeadDraw();

					using (var scrollView = new GUILayout.ScrollViewScope(_leftScrollPos, EditorStyles.helpBox))
					{
						_leftScrollPos = scrollView.scrollPosition;
						
						// 部分一致検索かフィルターが設定されていないときの表示
						if (_labelFilterOrFind || _labelFilterIndex == 0)
						{
							LabelFoldoutDraw();
						}
						else
						{
							LabelFilterResultDraw();
						}
					}
				}
			}

			return;

			void LabelObjectHeadDraw()
			{
				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField("◆アセット一覧",GUILayout.Width(100));
					if (GUILayout.Button("全て開く", GUILayout.Width(80), GUILayout.Height(20)))
					{
						var keys = _groupFoldout.Keys.ToList();
						foreach (var key in keys)
						{
							_groupFoldout[key] = true;
						}
					}
					if (GUILayout.Button("全て閉じる", GUILayout.Width(80), GUILayout.Height(20)))
					{
						var keys = _groupFoldout.Keys.ToList();
						foreach (var key in keys)
						{
							_groupFoldout[key] = false;
						}
					}
				}

				if (_labelList.Count == 0)
				{
					using (new EditorGUI.DisabledGroupScope(true))
					{
						EditorGUILayout.MaskField("フィルター", 0, new []{""}, GUILayout.MaxWidth(500));
					}
				}
				else
				{
					_labelFilterIndex = EditorGUILayout.MaskField("フィルター", _labelFilterIndex, _labelList.ToArray(), GUILayout.MaxWidth(500));
				}
				_labelFilterOrFind = EditorGUILayout.Toggle("部分一致", _labelFilterOrFind);
				for (var i = 0; i < _labelList.Count; i++)
				{
					_labelFilterToggleList[i] = (_labelFilterIndex & 1 << i) != 0;
				}
			}

			void LabelFoldoutDraw()
			{
				bool isEmpty = true;
				var keys = _groupFoldout.Keys.ToList();
				for (int i = 0; i < keys.Count; i++)
				{
					string key = keys[i];
					if (_labelFilterIndex != 0 && !_labelFilterToggleList[i])
					{
						continue;
					}
					var assetList = _labelAssetListDic[key];

					if (assetList.Count != 0)
					{
						bool isOpen = EditorGUILayout.Foldout(_groupFoldout[key], key);
						_groupFoldout[key] = isOpen;
								
						if(isOpen)
						{
							EditorGUI.indentLevel++;
							foreach (var prefab in assetList)
							{
								isEmpty = false;
								LabelAssetContentDraw(prefab, new List<string>(){key});
							}
							EditorGUI.indentLevel--;
						}
					}
				}

				if (isEmpty)
				{
					string emptyStr = keys.Count == 0 ? "ラベルが存在しません" : "フィルターの結果に一致するアセットが見つかりませんでした";
					LabelAssetNotFoundDraw(emptyStr);
				}
			}

			void LabelFilterResultDraw()
			{
				bool isEmpty = true;
				List<string> selectLabel = new List<string>();
				for (var i = 0; i < _labelFilterToggleList.Count; i++)
				{
					if (_labelFilterToggleList[i])
					{
						selectLabel.Add(_labelList[i]);
					}
				}

				foreach (var obj in _labelAssetListDic[selectLabel[0]])
				{
					var labels = AssetDatabase.GetLabels(obj);
					if (selectLabel.All(labels.Contains))
					{
						isEmpty = false;
						LabelAssetContentDraw(obj, selectLabel);
					}
				}

				if (isEmpty)
				{
					string emptyStr = selectLabel.Count == 0 ? "ラベルが存在しません" : "フィルターの結果に一致するアセットが見つかりませんでした";
					LabelAssetNotFoundDraw(emptyStr);
				}
			}

			void LabelAssetNotFoundDraw(string text)
			{
				GUILayout.FlexibleSpace();
				using (new GUILayout.HorizontalScope())
				{
					GUILayout.FlexibleSpace();
					GUILayout.Label(text);
					GUILayout.FlexibleSpace();
				}
				GUILayout.FlexibleSpace();
			}
			
			void LabelAssetContentDraw(Object prefab, List<string> keyList)
			{
				// 該当するグループのオブジェクト
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.ObjectField("", EditorUtility.InstanceIDToObject(prefab.GetInstanceID()), typeof(Object), false);

					// オブジェクトのグループを変更
					if (GUILayout.Button("選択", GUILayout.Width(60), GUILayout.Height(20)))
					{
						Selection.activeInstanceID = prefab.GetInstanceID();
					}
					
					// オブジェクトのグループを変更
					if (GUILayout.Button("変更", GUILayout.Width(60), GUILayout.Height(20)))
					{
						Selection.activeObject = prefab;
						AssetLabelDialog.ShowWindow();
					}
										
					// オブジェクトのグループを削除
					if (GUILayout.Button("削除", GUILayout.Width(60), GUILayout.Height(20)))
					{
						var labels = AssetDatabase.GetLabels(prefab);
						keyList.ForEach(x => ArrayUtility.Remove(ref labels, x));
						if (labels.Length == 0)
						{
							AssetDatabase.ClearLabels(prefab);
						}
						else
						{
							AssetDatabase.SetLabels(prefab, labels);
						}
						Initialize();
					}
				}
			}
		}

		void LabelListDraw()
		{
			void LabelListHeadDraw()
			{
				if (GUILayout.Button("ラベル情報を更新", GUILayout.Width(120), GUILayout.Height(20)))
				{
					var assetPathList = AssetDatabase.GetAllAssetPaths().ToList();
					List<string> labelList = new List<string>();
					labelList.AddRange(_assetsLabelList.LabelList);
						
					foreach (var assetPath in assetPathList)
					{
						if (assetPath.StartsWith("Packages/"))
						{
							continue;
						}
						var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
						var labels = AssetDatabase.GetLabels(obj);
						foreach (var label in labels)
						{
							if (!labelList.Contains(label))
							{
								labelList.Add(label);
							}
						}
					}

					_assetsLabelList.LabelList = labelList;
					_assetsLabelList.Save();
					Initialize();
				}
			}
			using (new GUILayout.VerticalScope(GUILayout.MaxWidth(330)))
			{
				LabelListHeadDraw();
				using (new GUILayout.VerticalScope(EditorStyles.helpBox))
				{
					using (new GUILayout.HorizontalScope())
					{
						GUILayout.Label("追加するラベル名:", GUILayout.Width(90));
						_addLabelName = GUILayout.TextField(_addLabelName);
						if (GUILayout.Button("追加", GUILayout.Width(50), GUILayout.Height(20)))
						{
							if (!string.IsNullOrEmpty(_addLabelName))
							{
								_assetsLabelList.LabelList.Add(_addLabelName);
								_assetsLabelList.Save();
								Initialize();
							}
						}
					}

					using (new GUILayout.VerticalScope(EditorStyles.helpBox))
					{
						using (new GUILayout.HorizontalScope())
						{
							EditorGUILayout.LabelField("◆ラベル一覧", GUILayout.Width(80));
							GUILayout.FlexibleSpace();
							if (GUILayout.Button("全て反映", GUILayout.Width(80), GUILayout.Height(20)))
							{
								LabelChangeApply(true);
							}
							if (GUILayout.Button("全て取り消し", GUILayout.Width(80), GUILayout.Height(20)))
							{
								LabelChangeRevert(true);
							}
							
						}
						using (new GUILayout.HorizontalScope())
						{
							// 全選択トグル制御
							bool isAllSelectCheck = _selectLabelToggleList.All(x => x);
							bool toggle = EditorGUILayout.ToggleLeft("全選択", isAllSelectCheck, GUILayout.Width(80), GUILayout.Height(20));
							bool isChange = isAllSelectCheck != toggle;
							if (isChange)
							{
								SelectLabelToggleAll(toggle);
							}
							
							GUILayout.FlexibleSpace();
							
							GUILayout.Label("一部変更:");
							
							if (GUILayout.Button("反映", GUILayout.Width(60), GUILayout.Height(20)))
							{
								LabelChangeApply(false);
							}

							if (GUILayout.Button("取り消し", GUILayout.Width(60), GUILayout.Height(20)))
							{
								LabelChangeRevert(false);
							}
							
							if (GUILayout.Button("削除", GUILayout.Width(60), GUILayout.Height(20)))
							{
								for (int i = 0; i < _selectLabelToggleList.Count; i++)
								{
									if (_selectLabelToggleList[i] && !_removeLabelIdxList.Contains(i))
									{
										_removeLabelIdxList.Add(i);
									}
								}

								SelectLabelToggleAll(false);
							}
						}
						

						using (var scrollView = new GUILayout.ScrollViewScope(_rightScrollPos, EditorStyles.helpBox))
						{
							_rightScrollPos = scrollView.scrollPosition;
							for (var i = 0; i < _labelList.Count; i++)
							{
								using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
								{
									_selectLabelToggleList[i] = EditorGUILayout.Toggle(_selectLabelToggleList[i],GUILayout.Width(15));
									GUILayout.Box("", GUILayout.Width(2));
									using (new EditorGUI.DisabledGroupScope(_removeLabelIdxList.Contains(i)))
									{
										var label = _labelList[i];
										if (label != _assetsLabelList.LabelList[i])
										{
											GUI.backgroundColor = Color.cyan;
										}
										_labelList[i] = EditorGUILayout.TextField(label);
										GUI.backgroundColor = _baseBackGroundColor;
									}
								}
							}
						}
					}
				}
			}
		}

		void LabelChangeApply(bool isAll)
		{
			var removeLabelList = _removeLabelIdxList.Select(x => _labelList[x]).ToList();
			removeLabelList.ForEach(x => _labelList.Remove(x));
			removeLabelList = _removeLabelIdxList.Select(x => _assetsLabelList.LabelList[x]).ToList();
			removeLabelList.ForEach(x => _assetsLabelList.LabelList.Remove(x));

			foreach (var removeLabel in removeLabelList)
			{
				var objList = _labelAssetListDic[removeLabel];
				_labelAssetListDic.Remove(removeLabel);

				foreach (var obj in objList)
				{
					var labels = AssetDatabase.GetLabels(obj);
					ArrayUtility.Remove(ref labels, removeLabel);
					if (labels.Length == 0)
					{
						AssetDatabase.ClearLabels(obj);
					}
					else
					{
						AssetDatabase.SetLabels(obj, labels);
					}
				}

			}
			
			if (isAll)
			{
				for (var i = 0; i < _assetsLabelList.LabelList.Count; i++)
				{
					string beforeLabel = _assetsLabelList.LabelList[i];
					string afterLabel = _labelList[i];
					_assetsLabelList.LabelList[i] = afterLabel;
 					foreach (var obj in _labelAssetListDic[beforeLabel])
					{
						var labels = AssetDatabase.GetLabels(obj);
						ArrayUtility.Remove(ref labels, beforeLabel);
						ArrayUtility.Add(ref labels, afterLabel);
						AssetDatabase.SetLabels(obj, labels);
					}
				}
			}
			else
			{
				int labelNum = _labelList.Count;
				for (int i = 0; i < labelNum; i++)
				{
					if(!_selectLabelToggleList[i]) continue;
					
					if (_labelList[i] != _assetsLabelList.LabelList[i])
					{
						var beforeLabel = _assetsLabelList.LabelList[i];
						var afterLabel = _labelList[i];
						_assetsLabelList.LabelList[i] = afterLabel;
						foreach (var obj in _labelAssetListDic[beforeLabel])
						{
							var labels = AssetDatabase.GetLabels(obj);
							ArrayUtility.Remove(ref labels, beforeLabel);
							ArrayUtility.Add(ref labels, afterLabel);
							AssetDatabase.SetLabels(obj, labels);
						}
					}
				}
			}

			_labelList.RemoveAll(string.IsNullOrEmpty);
			_assetsLabelList.Save();
			Initialize();
		}

		void LabelChangeRevert(bool isAll)
		{
			if (isAll)
			{
				_labelList = _assetsLabelList.LabelList.ToList();
				_removeLabelIdxList.Clear();
			}
			else
			{
				int labelNum = _labelList.Count;
				for (int i = 0; i < labelNum; i++)
				{
					if(!_selectLabelToggleList[i]) continue;
					
					_labelList[i] = _assetsLabelList.LabelList[i];
					_removeLabelIdxList.Remove(i);
				}
			}
			
			_labelList.RemoveAll(string.IsNullOrEmpty);
		}
		
		
		void SelectLabelToggleAll(bool isCheck)
		{
			for (int i = 0; i < _selectLabelToggleList.Count; i++)
			{
				_selectLabelToggleList[i] = isCheck;
			}
		}
	}
}
