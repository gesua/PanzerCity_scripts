using UnityEngine;
using UnityEditor;

public class ItemConfigImporter : EditorWindow
{
    [MenuItem("Tools/Import Item Config")]
    static void ImportItemConfig()
    {
        // JSON 로드
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/Item_Master");
        ItemMasterList masterList = JsonUtility.FromJson<ItemMasterList>(jsonFile.text);

        foreach (ItemMasterData data in masterList.list)
        {
            // 기존 ItemConfig 찾기
            string path = $"Assets/Resources/Items/{data.ItemID}{data.ItemName}.asset";
            ItemConfig config = AssetDatabase.LoadAssetAtPath<ItemConfig>(path);

            // 없으면 새로 생성
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ItemConfig>();
                AssetDatabase.CreateAsset(config, path);
            }

            // 데이터 채워넣기
            // SerializedObject로 접근해야 private SerializeField 수정 가능
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("_id").intValue = data.ItemID;
            so.FindProperty("_itemName").stringValue = data.ItemName;
            so.FindProperty("_nameKey").stringValue = data.NameKey;
            so.FindProperty("_descKey").stringValue = data.DescKey;
            so.FindProperty("_buyPrice").intValue = data.BuyPrice;
            so.FindProperty("_skill").stringValue = data.Skill;

            ItemType itemType = System.Enum.Parse<ItemType>(data.ItemType);
            so.FindProperty("_itemType").enumValueIndex = (int)itemType;

            // EquipSlot 변환 (빈 문자열이면 None)
            EquipSlot equipSlot = string.IsNullOrEmpty(data.EquipSlot)
                ? EquipSlot.None
                : System.Enum.Parse<EquipSlot>(data.EquipSlot);
            so.FindProperty("_equipSlot").enumValueIndex = (int)equipSlot;

            so.ApplyModifiedProperties();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ItemConfig 임포트 완료");
    }
}