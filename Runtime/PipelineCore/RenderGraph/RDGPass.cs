﻿using System;
using System.Collections.Generic;

namespace InfinityTech.Rendering.RDG
{
    abstract class IRDGPass
    {
        public abstract void Execute(ref RDGContext graphContext);
        public abstract void Release(RDGObjectPool objectPool);
        public abstract bool HasRenderFunc();

        public string name;
        public int index;
        public UnityEngine.Rendering.ProfilingSampler customSampler;
        public bool             enableAsyncCompute { get; protected set; }
        public bool             allowPassCulling { get; protected set; }

        public RDGTextureRef depthBuffer { get; protected set; }
        public RDGTextureRef[]  colorBuffers { get; protected set; } = new RDGTextureRef[8];
        public int              colorBufferMaxIndex { get; protected set; } = -1;
        public int              refCount { get; protected set; }

        public List<RDGResourceRef>[] resourceReadLists = new List<RDGResourceRef>[2];
        public List<RDGResourceRef>[] resourceWriteLists = new List<RDGResourceRef>[2];
        public List<RDGResourceRef>[] temporalResourceList = new List<RDGResourceRef>[2];


        public IRDGPass()
        {
            for (int i = 0; i < 2; ++i)
            {
                resourceReadLists[i] = new List<RDGResourceRef>();
                resourceWriteLists[i] = new List<RDGResourceRef>();
                temporalResourceList[i] = new List<RDGResourceRef>();
            }
        }

        public void AddResourceWrite(in RDGResourceRef res)
        {
            resourceWriteLists[res.iType].Add(res);
        }

        public void AddResourceRead(in RDGResourceRef res)
        {
            resourceReadLists[res.iType].Add(res);
        }

        public void AddTemporalResource(in RDGResourceRef res)
        {
            temporalResourceList[res.iType].Add(res);
        }

        public void SetColorBuffer(RDGTextureRef resource, int index)
        {
            colorBufferMaxIndex = Math.Max(colorBufferMaxIndex, index);
            colorBuffers[index] = resource;
            AddResourceWrite(resource.handle);
        }

        public void SetDepthBuffer(RDGTextureRef resource, EDepthAccess flags)
        {
            depthBuffer = resource;
            if ((flags & EDepthAccess.Read) != 0)
                AddResourceRead(resource.handle);
            if ((flags & EDepthAccess.Write) != 0)
                AddResourceWrite(resource.handle);
        }

        public void EnableAsyncCompute(bool value)
        {
            enableAsyncCompute = value;
        }

        public void AllowPassCulling(bool value)
        {
            allowPassCulling = value;
        }

        public void Clear()
        {
            name = "";
            index = -1;
            customSampler = null;
            for (int i = 0; i < 2; ++i)
            {
                resourceReadLists[i].Clear();
                resourceWriteLists[i].Clear();
                temporalResourceList[i].Clear();
            }

            refCount = 0;
            allowPassCulling = true;
            enableAsyncCompute = false;

            // Invalidate everything
            colorBufferMaxIndex = -1;
            depthBuffer = new RDGTextureRef();
            for (int i = 0; i < 8; ++i)
            {
                colorBuffers[i] = new RDGTextureRef();
            }
        }

    }

    public delegate void FExecuteAction<T>(ref T passData, ref RDGContext graphContext) where T : struct;

    internal sealed class RDGPass<T> : IRDGPass where T : struct
    {
        internal T passData;
        internal FExecuteAction<T> ExcuteFunc;

        public override void Execute(ref RDGContext graphContext)
        {
            ExcuteFunc(ref passData, ref graphContext);
        }

        public override void Release(RDGObjectPool objectPool)
        {
            Clear();
            ExcuteFunc = null;
            objectPool.Release(this);
        }

        public override bool HasRenderFunc()
        {
            return ExcuteFunc != null;
        }
    }
}