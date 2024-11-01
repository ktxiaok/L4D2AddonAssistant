﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace L4D2AddonAssistant
{
    public class AddonNode : ObservableObject, IHierarchyNode<AddonNode>
    {
        private bool _isValid = true;

        private bool _isEnabled = false;

        private bool _allowEnabledInHierarchy = true;

        private string _name = "";

        private AddonGroup? _group = null;

        private AddonRoot _root;

        private List<AddonProblem>? _problems = null;

        public AddonNode(AddonRoot root, AddonGroup? group = null)
        {
            OnInitSelf();

            ArgumentNullException.ThrowIfNull(root);
            if (group != null && group.Root != root)
            {
                ThrowDifferentRootException();
            }

            _root = root;
            if (group == null)
            {
                root.AddNode(this);
            }
            else
            {
                group.AddChild(this);
                Group = group;
            }
            UpdateEnabledInHierarchy();
        }

        public virtual Type SaveType => typeof(AddonNodeSave);

        public bool IsValid => _isValid;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (value == _isEnabled)
                {
                    return;
                }
                _isEnabled = value;
                NotifyChanged();
                UpdateEnabledInHierarchy();
                if (Group != null)
                {
                    Group.NotifyChildEnableOrDisable(this);
                }
                AutoCheck();
                Root.RequestSave = true;
            }
        }

        public bool IsEnabledInHierarchy => _allowEnabledInHierarchy && _isEnabled;

        public AddonGroup? Group
        {
            get => _group;
            private set
            {
                if (NotifyAndSetIfChanged(ref _group, value))
                {
                    Root.RequestSave = true;
                }
            }
        }

        public IAddonNodeContainer Parent => ((IAddonNodeContainer?)Group) ?? Root; 

        public AddonRoot Root => _root;

        public virtual bool RequireFile => false;

        public bool IsAutoCheck => Root?.IsAutoCheck ?? true; 

        bool IHierarchyNode<AddonNode>.IsNonterminal => HasChildren;

        public ReadOnlyObservableCollection<AddonNode> Children => Children_Internal;

        IEnumerable<AddonNode> IHierarchyNode<AddonNode>.Children => Children;

        public bool HasChildren => HasChildren_Internal;

        public bool HasProblem
        {
            get
            {
                if (_problems == null)
                {
                    return false;
                }
                return _problems.Count > 0;
            }
        }

        public IReadOnlyCollection<AddonProblem> Problems
        {
            get
            {
                if (_problems == null)
                {
                    return ReadOnlyCollection<AddonProblem>.Empty;
                }
                return _problems;
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                if (value.Length == 0)
                {
                    throw new ArgumentException("The value is empty.");
                }
                if (value == _name)
                {
                    return;
                }

                var parentInternal = (IAddonNodeContainerInternal)Parent;
                parentInternal.ThrowIfNodeNameInvalid(value);

                // Try to move the file.
                if (RequireFile && _name.Length > 0)
                {
                    string sourcePath = BuildFilePath(Group, FileName);
                    string fullSourcePath = GetFullFilePath(sourcePath);
                    if (FileUtils.Exists(fullSourcePath))
                    {
                        string targetPath = Path.Join(Path.GetDirectoryName(sourcePath) ?? "", value + FileExtension);
                        string fullTargetPath = GetFullFilePath(targetPath);
                        if (FileUtils.Exists(fullTargetPath))
                        {
                            throw new FileExistException(fullTargetPath);
                        }
                        FileUtils.Move(fullSourcePath, fullTargetPath);
                    }
                }

                parentInternal.ChangeNameUnchecked(_name, value, this);
                _name = value;
                NotifyChanged();
                Root.RequestSave = true;
            }
        }

        public string FullName => BuildFilePath(Group, Name);

        public string FileName => Name + FileExtension;

        public string FilePath
        {
            get => BuildFilePath(Group, FileName);
        }

        public string FullFilePath => GetFullFilePath(FilePath);

        public virtual string FileExtension => "";

        internal virtual ReadOnlyObservableCollection<AddonNode> Children_Internal => throw new NotSupportedException();

        internal virtual bool HasChildren_Internal => false;

        public virtual byte[]? RetrieveImage()
        {
            return null;
        }

        public virtual void InvalidateImage()
        {

        }

        public bool CanMoveTo(AddonGroup? group)
        {
            if (group == null)
            {
                return true;
            }
            if (Root != group.Root)
            {
                throw new InvalidOperationException("Different root.");
            }
            if (!HasChildren)
            {
                return true;
            }
            foreach (var node in this.GetSelfAndDescendantsByDfsPreorder())
            {
                if (node == group)
                {
                    return false;
                }
            }
            return true;
        }

        public void MoveTo(AddonGroup? group)
        {
            // Check the argument.
            if (group != null && Root != group.Root)
            {
                ThrowDifferentRootException();
            }
            if (!CanMoveTo(group))
            {
                ThrowMoveGroupToItselfException();
            }
            if (group == Group)
            {
                return;
            }

            var containerInternal = group == null ? (IAddonNodeContainerInternal)Root : (IAddonNodeContainerInternal)group;
            containerInternal.ThrowIfNodeNameInvalid(Name);

            // Try to move the file.
            if (RequireFile && Name.Length > 0)
            {
                string fileName = FileName;
                string sourcePath = BuildFilePath(Group, fileName);
                string fullSourcePath = GetFullFilePath(sourcePath);
                if (FileUtils.Exists(fullSourcePath))
                {
                    string targetPath = BuildFilePath(group, fileName);
                    string fullTargetPath = GetFullFilePath(targetPath);
                    FileUtils.Move(fullSourcePath, fullTargetPath);
                }
            }

            if (group == null)
            {
                if (Group == null)
                {
                    return;
                }
                Group.RemoveChild(this);
                Group = null;
                Root.AddNode(this);
            }
            else
            {
                if (Group == null)
                {
                    Root.RemoveNode(this);
                }
                else
                {
                    Group.RemoveChild(this);
                }
                group.AddChild(this);
                Group = group;
            }

            UpdateEnabledInHierarchy();
        }

        public void DeleteFile()
        {
            if (RequireFile)
            {
                string path = FullFilePath;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
        }

        public void Destroy()
        {
            foreach (var node in this.GetSelfAndDescendantsByDfsPreorder())
            {
                node.OnDestroy();
            }
            if (Group == null)
            {
                Root.RemoveNode(this);
            }
            else
            {
                Group.RemoveChild(this);
            }
        }

        public void Check()
        {
            if (_problems != null)
            {
                _problems.Clear();
            }
            OnCheck(AddProblem);
            if (_problems != null)
            {
                if (_problems.Count == 0)
                {
                    _problems = null;
                }
            }

            void AddProblem(AddonProblem problem)
            {
                if (_problems == null)
                {
                    _problems = new(2);
                }
                _problems.Add(problem);
            }
        }

        private class CreateSaveStackFrame
        {
            public AddonGroupSave Save;
            public IEnumerator<AddonNode> Children;
            public List<AddonNodeSave> SaveChildren = new();
            
            public CreateSaveStackFrame(AddonGroupSave save, IEnumerator<AddonNode> children)
            {
                Save = save;
                Children = children;
            }
        }

        public AddonNodeSave CreateSave()
        {
            var save = NewSave(this);
            OnCreateSave(save);
            if (!HasChildren)
            {
                return save;
            }
            List<CreateSaveStackFrame> stack = new();
            stack.Add(new CreateSaveStackFrame((AddonGroupSave)save, Children.GetEnumerator()));
            while (stack.Count != 0)
            {
                var current = stack[stack.Count - 1];
                if (current.Children.MoveNext())
                {
                    var child = current.Children.Current;
                    var childSave = NewSave(child);
                    child.OnCreateSave(childSave);
                    current.SaveChildren.Add(childSave);
                    if (child.HasChildren)
                    {
                        stack.Add(new CreateSaveStackFrame((AddonGroupSave)childSave, child.Children.GetEnumerator()));
                    }
                }
                else
                {
                    current.Save.Children = current.SaveChildren.ToArray();
                    stack.RemoveAt(stack.Count - 1);
                }
            }
            return save;

            AddonNodeSave NewSave(AddonNode node)
            {
                return (AddonNodeSave)Activator.CreateInstance(node.SaveType)!;
            }
        }

        private class LoadSaveStackFrame
        {
            public AddonGroup Group;
            public AddonGroupSave Save;
            public int ChildIdx = 0;

            public LoadSaveStackFrame(AddonGroup group, AddonGroupSave save)
            {
                Group = group;
                Save = save;
            }
        }

        public static AddonNode LoadSave(AddonNodeSave save, AddonRoot root)
        {
            var node = NewNode(save, null);
            node.OnLoadSave(save);
            var groupSave = save as AddonGroupSave;
            if (groupSave == null)
            {
                return node;
            }

            List<LoadSaveStackFrame> stack = new();
            stack.Add(new LoadSaveStackFrame((AddonGroup)node, groupSave));
            while (stack.Count != 0)
            {
                var current = stack[stack.Count - 1];
                if (current.ChildIdx < current.Save.Children.Length)
                {
                    var childSave = current.Save.Children[current.ChildIdx];
                    ++current.ChildIdx;
                    var child = NewNode(childSave, current.Group);
                    child.OnLoadSave(childSave);
                    if (child is AddonGroup childGroup)
                    {
                        stack.Add(new LoadSaveStackFrame(childGroup, (AddonGroupSave)childSave));
                    }
                }
                else
                {
                    stack.RemoveAt(stack.Count - 1);
                }
            }

            return node;

            AddonNode NewNode(AddonNodeSave save, AddonGroup? group)
            {
                return (AddonNode)Activator.CreateInstance(save.TargetType, root, group)!;
            }
        }

        protected void AutoCheck()
        {
            if (IsAutoCheck)
            {
                Check();
            }
        }

        protected virtual void OnDestroy()
        {
            _isValid = false;
            NotifyChanged(nameof(IsValid));
        }

        protected virtual void OnCheck(Action<AddonProblem> submiter)
        {

        }

        protected virtual void OnCreateSave(AddonNodeSave save)
        {
            save.IsEnabled = IsEnabled;
            save.Name = Name;
        }

        protected virtual void OnLoadSave(AddonNodeSave save)
        {
            IsEnabled = save.IsEnabled;
            Name = save.Name;
        }

        internal virtual void OnInitSelf()
        {

        }

        internal static string BuildFilePath(AddonGroup? group, string name)
        {
            if (group == null)
            {
                return name;
            }
            var nameList = new List<string>();
            nameList.Add(name);
            AddonGroup? current = group;
            while (current != null)
            {
                nameList.Add(current.Name);
                current = current.Group;
            }
            nameList.Reverse();
            return Path.Join(nameList.ToArray());
        }

        internal string GetFullFilePath(string path)
        {
            return Path.Join(Root.DirectoryPath, path);
        }

        private void UpdateEnabledInHierarchy()
        {
            var dfs = this.GetSelfAndDescendantsEnumByDfsPreorder();
            while (dfs.MoveNext())
            {
                var current = dfs.Current;
                var parent = current.Group;
                bool oldCurrentEnabledInHierarchy = current.IsEnabledInHierarchy;
                current._allowEnabledInHierarchy = parent == null ? true : parent.IsEnabledInHierarchy;
                if (current != this && current.IsEnabledInHierarchy == oldCurrentEnabledInHierarchy)
                {
                    dfs.SkipDescendantsOfCurrent();
                }
                else
                {
                    current.NotifyChanged(nameof(IsEnabledInHierarchy));
                }
            }
        }

        private static void ThrowDifferentRootException()
        {
            throw new InvalidOperationException("Couldn't move to a group whose root is different!");
        }

        private static void ThrowMoveGroupToItselfException()
        {
            throw new InvalidOperationException("Couldn't move a group to itself!");
        }
    }
}
