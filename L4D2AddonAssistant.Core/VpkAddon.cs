﻿using Serilog;
using SteamDatabase.ValvePak;
using System;
using System.Diagnostics.CodeAnalysis;

namespace L4D2AddonAssistant
{
    public abstract class VpkAddon : AddonNode
    {
        private WeakReference<VpkAddonInfo?> _addonInfo = new(null);

        public VpkAddon(AddonRoot root, AddonGroup? group) : base(root, group)
        {
            
        }

        public abstract string? FullVpkFilePath
        {
            get;
        }

        public override Type SaveType => typeof(VpkAddonSave);

        public override bool RequireFile => true;

        protected override long? GetFileSize()
        {
            var path = FullVpkFilePath;
            if (path == null)
            {
                return null;
            }

            try
            {
                if (File.Exists(path))
                {
                    return new FileInfo(path).Length;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred during VpkAddon.GetFileSize.");
            }

            return null;
        }

        protected override Task<byte[]?> DoGetImageAsync(CancellationToken cancellationToken)
        {
            string? vpkPath = FullVpkFilePath;
            if (vpkPath == null)
            {
                return Task.FromResult<byte[]?>(null);
            }
            return Task.Run(() =>
            {
                if (TryCreatePackage(vpkPath, out var pak))
                {
                    using (pak)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return VpkUtils.GetAddonImage(pak);
                    }
                }
                return null;
            }, cancellationToken);
        }

        public VpkAddonInfo? RetrieveInfo()
        {
            if (!_addonInfo.TryGetTarget(out var addonInfo))
            {
                if (TryCreatePackage(FullVpkFilePath, out var pak))
                {
                    using (pak)
                    {
                        addonInfo = VpkUtils.GetAddonInfo(pak);
                        if (addonInfo != null)
                        {
                            _addonInfo.SetTarget(addonInfo);
                        }
                    }
                }
            }
            return addonInfo;
        }

        public override void ClearCaches()
        {
            base.ClearCaches();

            _addonInfo.SetTarget(null);
        }

        protected override void OnCreateSave(AddonNodeSave save)
        {
            base.OnCreateSave(save);
            var save1 = (VpkAddonSave)save;
        }

        protected override void OnLoadSave(AddonNodeSave save)
        {
            base.OnLoadSave(save);
            var save1 = (VpkAddonSave)save;
        }

        private static bool TryCreatePackage(string? path, [NotNullWhen(true)] out Package? pak)
        {
            pak = null;
            if (path == null)
            {
                return false;
            }
            try
            {
                pak = new Package();
                pak.Read(path);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception in VpkAddon.TryCreatePackage.");
                if (pak != null)
                {
                    pak.Dispose();
                    pak = null;
                }
                return false;
            }
        }
    }
}
