using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class CodeCopyer : EditorWindow
{
    // --- 数据结构：树形节点 ---
    private class FolderNode
    {
        public string Name;             // 文件夹名
        public string FullPath;         // 完整路径
        public bool IsSelected;         // 是否被勾选
        public bool IsExpanded = true;  // UI折叠状态
        public bool HasCodeFiles;       // 当前文件夹下是否有代码文件（不含子目录）
        public List<FolderNode> Children = new List<FolderNode>(); // 子文件夹
        public FolderNode Parent;       // 父节点引用
    }

    private FolderNode _rootNode;       // 树的根节点
    private Vector2 _scrollPosition;
    private string _extractedCode = "";
    private bool _hasScanned = false;

    [MenuItem("Tools/Code Copyer")]
    public static void ShowWindow()
    {
        GetWindow<CodeCopyer>("Code Copyer");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("代码提取助手 (树形修正版)", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // 1. 扫描区域
        if (GUILayout.Button("扫描 Assets/Scripts 目录", GUILayout.Height(30)))
        {
            ScanFoldersTree();
        }

        GUILayout.Space(10);

        // 2. 树形列表区域
        if (_hasScanned && _rootNode != null)
        {
            GUILayout.Label("请选择要提取的模块:", EditorStyles.label);

            // 全选/反选工具栏
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("全选", GUILayout.Width(60))) SetTreeSelection(_rootNode, true);
            if (GUILayout.Button("全不选", GUILayout.Width(60))) SetTreeSelection(_rootNode, false);
            if (GUILayout.Button("全部展开", GUILayout.Width(70))) SetTreeExpansion(_rootNode, true);
            if (GUILayout.Button("全部折叠", GUILayout.Width(70))) SetTreeExpansion(_rootNode, false);
            GUILayout.EndHorizontal();

            // 滚动视图
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, "box", GUILayout.Height(300));
            DrawFolderNode(_rootNode, 0); // 递归绘制树
            GUILayout.EndScrollView();
        }
        else if (_hasScanned && _rootNode == null)
        {
            GUILayout.Label("未找到包含代码的文件夹。");
        }

        GUILayout.Space(10);

        // 3. 提取操作区域
        if (GUILayout.Button("提取并压缩代码", GUILayout.Height(30)))
        {
            ExtractCodesFromTree();
        }

        GUILayout.Space(10);

        // 4. 结果与复制
        GUILayout.Label("代码存放区:", EditorStyles.boldLabel);
        var textAreaStyle = new GUIStyle(EditorStyles.textArea);
        textAreaStyle.wordWrap = true;
        _extractedCode = EditorGUILayout.TextArea(_extractedCode, textAreaStyle, GUILayout.Height(150));

        GUILayout.Space(5);

        if (GUILayout.Button("Copy All (复制到剪贴板)", GUILayout.Height(40)))
        {
            if (!string.IsNullOrEmpty(_extractedCode))
            {
                EditorGUIUtility.systemCopyBuffer = _extractedCode;
                EditorUtility.DisplayDialog("成功", "代码已复制到剪贴板！", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "没有可复制的内容", "OK");
            }
        }
    }

    // --- 核心逻辑：构建树 ---
    private void ScanFoldersTree()
    {
        string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");

        if (!Directory.Exists(scriptsRoot))
        {
            EditorUtility.DisplayDialog("错误", "未找到 'Assets/Scripts' 文件夹！", "OK");
            return;
        }

        _rootNode = BuildDirectoryNode(scriptsRoot, null);

        if (_rootNode != null && !_rootNode.HasCodeFiles && _rootNode.Children.Count == 0)
        {
            _rootNode = null;
        }

        _hasScanned = true;
    }

    private FolderNode BuildDirectoryNode(string path, FolderNode parent)
    {
        bool hasCsFiles = Directory.GetFiles(path, "*.cs", SearchOption.TopDirectoryOnly).Length > 0;
        string[] subDirs = Directory.GetDirectories(path);
        List<FolderNode> childrenNodes = new List<FolderNode>();

        foreach (var dir in subDirs)
        {
            FolderNode child = BuildDirectoryNode(dir, null);
            if (child != null)
            {
                childrenNodes.Add(child);
            }
        }

        if (!hasCsFiles && childrenNodes.Count == 0)
        {
            return null;
        }

        FolderNode node = new FolderNode
        {
            Name = new DirectoryInfo(path).Name,
            FullPath = path,
            IsSelected = true,
            IsExpanded = true,
            HasCodeFiles = hasCsFiles,
            Children = childrenNodes,
            Parent = parent
        };

        foreach (var child in childrenNodes)
        {
            child.Parent = node;
        }

        return node;
    }

    // --- 核心逻辑：递归绘制 UI ---
    private void DrawFolderNode(FolderNode node, int indentLevel)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(indentLevel * 20);

        if (node.Children.Count > 0)
        {
            node.IsExpanded = EditorGUILayout.Foldout(node.IsExpanded, GUIContent.none, true);
        }
        else
        {
            GUILayout.Space(13);
        }

        bool prevSelected = node.IsSelected;
        string displayName = node.Name + (node.HasCodeFiles ? " (含代码)" : "");

        // 绘制 Toggle
        node.IsSelected = EditorGUILayout.ToggleLeft(displayName, node.IsSelected);

        // 如果点击了父级，联动子级
        if (node.IsSelected != prevSelected)
        {
            SetTreeSelection(node, node.IsSelected);
        }

        GUILayout.EndHorizontal();

        if (node.IsExpanded)
        {
            foreach (var child in node.Children)
            {
                DrawFolderNode(child, indentLevel + 1);
            }
        }
    }

    private void SetTreeSelection(FolderNode node, bool select)
    {
        if (node == null) return;
        node.IsSelected = select;
        foreach (var child in node.Children)
        {
            SetTreeSelection(child, select);
        }
    }

    private void SetTreeExpansion(FolderNode node, bool expand)
    {
        if (node == null) return;
        node.IsExpanded = expand;
        foreach (var child in node.Children)
        {
            SetTreeExpansion(child, expand);
        }
    }

    // --- 核心逻辑：提取代码 (修复 Bug 处) ---
    private void ExtractCodesFromTree()
    {
        if (_rootNode == null) return;

        StringBuilder sb = new StringBuilder();
        int totalFiles = 0;

        CollectCodesRecursive(_rootNode, sb, ref totalFiles);

        _extractedCode = sb.ToString();

        if (totalFiles == 0)
        {
            _extractedCode = "未选择任何包含代码的文件夹。";
        }
        else
        {
            Debug.Log($"提取完成：共处理 {totalFiles} 个文件。");
        }
    }

    private void CollectCodesRecursive(FolderNode node, StringBuilder sb, ref int count)
    {
        // [修复点]：不要在这里写 if (!node.IsSelected) return;
        // 即使当前文件夹没被选中，也要继续向下遍历，因为子文件夹可能被选中了！

        // 1. 如果当前文件夹被选中，且有代码，提取它
        if (node.IsSelected && node.HasCodeFiles)
        {
            string[] files = Directory.GetFiles(node.FullPath, "*.cs", SearchOption.TopDirectoryOnly);
            foreach (string path in files)
            {
                ProcessFile(path, sb);
                count++;
            }
        }

        // 2. 无论当前文件夹是否选中，都要递归检查子节点
        foreach (var child in node.Children)
        {
            CollectCodesRecursive(child, sb, ref count);
        }
    }

    private void ProcessFile(string path, StringBuilder sb)
    {
        // 强制 UTF-8 读取
        string content = File.ReadAllText(path, Encoding.UTF8);
        string compressedContent = CompressCode(content);

        // 获取相对路径
        string relativePath = path.Replace(Application.dataPath, "Assets");

        sb.Append($"\n[FILE START: {relativePath}]\n");
        sb.Append(compressedContent);
        sb.Append($"\n[FILE END]\n");
    }

    private string CompressCode(string source)
    {
        string noBlockComments = Regex.Replace(source, @"/\*[\s\S]*?\*/", "");
        string noLineComments = Regex.Replace(noBlockComments, @"//.*", "");
        string singleLine = Regex.Replace(noLineComments, @"\s+", " ");

        return singleLine.Trim();
    }
}