using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
public class SubclassSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 只有标记了 [SerializeReference] 的字段才处理
        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        // 1. 绘制左侧的 Label
        var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(labelRect, label);

        // 2. 绘制右侧的类型选择下拉按钮
        var buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);

        string typeName = GetTypeName(property);
        if (EditorGUI.DropdownButton(buttonRect, new GUIContent(typeName), FocusType.Passive))
        {
            ShowTypeMenu(property);
        }

        // 3. 绘制属性本身的子内容（展开后的字段）
        EditorGUI.PropertyField(position, property, GUIContent.none, true);
    }

    private void ShowTypeMenu(SerializedProperty property)
    {
        var menu = new GenericMenu();

        // --- 修复开始：使用 Unity 提供的原生 API 获取准确的基类类型 ---
        // managedReferenceFieldTypename 格式通常为 "Assembly-Name Type-Full-Name"
        string typeName = property.managedReferenceFieldTypename;
        if (string.IsNullOrEmpty(typeName)) return;

        var splitTypeString = typeName.Split(' ');
        var assemblyName = splitTypeString[0];
        var className = splitTypeString[1];

        // 从当前域中找到对应的 Assembly 和 Type
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);

        if (assembly == null)
        {
            Debug.LogError($"[SubclassSelector] 找不到 Assembly: {assemblyName}");
            return;
        }

        Type baseType = assembly.GetType(className);
        if (baseType == null)
        {
            Debug.LogError($"[SubclassSelector] 找不到类型: {className}");
            return;
        }
        // --- 修复结束 ---

        // 自动反射查找所有子类
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => baseType.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);

        menu.AddItem(new GUIContent("None (Null)"), false, () => {
            property.managedReferenceValue = null;
            property.serializedObject.ApplyModifiedProperties();
        });

        foreach (var type in types)
        {
            // 使用 type.FullName 确保同名类（不同命名空间）也能区分
            string menuLabel = type.Name;

            // 如果有命名空间，也可以加上，看你喜好
            // string menuLabel = string.IsNullOrEmpty(type.Namespace) ? type.Name : $"{type.Namespace}/{type.Name}";

            menu.AddItem(new GUIContent(menuLabel), false, () => {
                // 必须使用无参构造函数
                property.managedReferenceValue = Activator.CreateInstance(type);
                property.serializedObject.ApplyModifiedProperties();
            });
        }
        menu.ShowAsContext();
    }

    private string GetTypeName(SerializedProperty property)
    {
        if (string.IsNullOrEmpty(property.managedReferenceFullTypename)) return "Select Type...";
        return property.managedReferenceFullTypename.Split('.').Last();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // 必须返回内容的高度，否则字段会重叠
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}