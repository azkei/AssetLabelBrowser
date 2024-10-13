using UnityEditor;
using UnityEngine;

namespace AssetLabelUtil
{
	public static class AssetLabelProjectEditor
	{
		const string LabelTogglePath = "Tools/azki-soft/ShowLabel";
		const string LabelTogglePrefsPath = "azk_ShowLabel";
	
		[InitializeOnLoadMethod]
		static void Initialize()
		{
			string enable = EditorUserSettings.GetConfigValue(LabelTogglePrefsPath);
			if (!string.IsNullOrEmpty(enable))
			{
				_showLabel = enable == "1" ? true : false;
			}
			else
			{
				_showLabel = false;
				enable = "0";
			}
			
			EditorUserSettings.SetConfigValue(LabelTogglePrefsPath, enable);
			
			EditorApplication.projectWindowItemOnGUI += OnGUI;
		}

		[MenuItem(LabelTogglePath)]
		public static void ShowLabelToggle()
		{
			_showLabel = !_showLabel;
			string enable = _showLabel ? "1" : "0";
			EditorUserSettings.SetConfigValue(LabelTogglePrefsPath, enable);
			Menu.SetChecked(LabelTogglePath, _showLabel);
		}

        [MenuItem(LabelTogglePath, validate = true)]
        static bool ShowLabelToggleValidator()
        {
            Menu.SetChecked(LabelTogglePath, _showLabel);
            return true;
        }

        static bool _showLabel = false;
		static readonly GUIContent CacheContent = new GUIContent();
		static Vector2 _cacheSize;
		static GUIStyle _cacheStyle;
		const int Offset = 5;

		static void OnGUI(string guid, Rect rect)
		{
			if (string.IsNullOrEmpty(guid) || !_showLabel)
				return;

			string[] labels = AssetDatabase.GetLabels(new GUID(guid));			

			if (_cacheStyle == null)
				_cacheStyle = "AssetLabel";

			float posx = rect.xMax;
		
			foreach (var t in labels)
			{
				CacheContent.text = t;
				_cacheSize = _cacheStyle.CalcSize(CacheContent);
				_cacheSize.x += Offset;
				posx -= _cacheSize.x + Offset;
				Rect tmpRect = new Rect(posx, rect.y, _cacheSize.x, _cacheSize.y); 
			
				GUI.Label(tmpRect, CacheContent, _cacheStyle);
			}
		}
	}
}
