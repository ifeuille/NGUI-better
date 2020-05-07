using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[RequireComponent(typeof(ParticleSystem))]
public class ParticleSystemExt : MonoBehaviour
{
    // Cached and saved values
    [HideInInspector] [SerializeField] UIAtlas mAtlas;
    [HideInInspector] [SerializeField] string mSpriteName;

    [System.NonSerialized] protected UISpriteData mSprite;
    [System.NonSerialized] protected int index;    

    /// <summary>
    /// Atlas used by this widget.
    /// </summary>
    public UIAtlas atlas
    {
        get
        {
            return mAtlas;
        }
        set
        {
            if (mAtlas != value)
            {

                mAtlas = value;
                mSprite = null;

                // Automatically choose the first sprite
                if (string.IsNullOrEmpty(mSpriteName))
                {
                    if (mAtlas != null && mAtlas.spriteList.Count > 0)
                    {
                        SetAtlasSprite(mAtlas.spriteList[0]);
                        mSpriteName = mSprite.name;
                    }
                }

                // Re-link the sprite
                if (!string.IsNullOrEmpty(mSpriteName))
                {
                    string sprite = mSpriteName;
                    mSpriteName = "";
                    spriteName = sprite;
                }
            }
        }
    }

    /// <summary>
    /// Sprite within the atlas used to draw this widget.
    /// </summary>
    public string spriteName
    {
        get
        {
            return mSpriteName;
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                // If the sprite name hasn't been set yet, no need to do anything
                if (string.IsNullOrEmpty(mSpriteName)) return;

                // Clear the sprite name and the sprite reference
                mSpriteName = "";
                mSprite = null;
            }
            else if (mSpriteName != value)
            {
                // If the sprite name changes, the sprite reference should also be updated
                mSpriteName = value;
                UISpriteData sp = mAtlas.GetSprite(mSpriteName);
                if (sp == null) return;
                SetAtlasSprite(sp);
            }
        }
    }

    protected void SetAtlasSprite(UISpriteData sp)
    {
        if (sp != null)
        {
            mSprite = sp;
            mSpriteName = mSprite.name;
        }
        else
        {
            mSpriteName = (mSprite != null) ? mSprite.name : "";
            mSprite = sp;
        }
        RefreshParticleSystem();
    }

    public void RefreshParticleSystem()
    {
        ParticleSystem ps = gameObject.GetComponent<ParticleSystem>();
        if(ps == null)
        {
            Debug.LogError("No ParticleSystem");
            return;
        }
        if (mAtlas == null) return;
        UISpriteData sp = mAtlas.GetSprite(mSpriteName);
        if (sp == null)
        {
            Debug.LogErrorFormat("物体'{0}'引用图元'{1}'没有找到", gameObject.name, mSpriteName);
            return;
        }
        mSprite = sp;
        int x = mSprite.x;
        int y = mSprite.y;
        int width = mSprite.width;
        int height = mSprite.height;
        int bigWidth = mAtlas.texture.width;
        int bigHeight = mAtlas.texture.height;
        int numInLine = bigWidth / width;
        index = (y / height) * numInLine + x / width;
        int numInHeight = bigHeight / height;
        int num = numInHeight * numInLine;
        ParticleSystem.TextureSheetAnimationModule tsa = ps.textureSheetAnimation;
        tsa.enabled = true;
        tsa.mode = ParticleSystemAnimationMode.Grid;
        tsa.animation = ParticleSystemAnimationType.WholeSheet;
        tsa.numTilesX = numInLine;
        tsa.numTilesY = numInHeight;
        var frameOverTimeCurve = tsa.frameOverTime;
        frameOverTimeCurve.mode = ParticleSystemCurveMode.Constant;
        frameOverTimeCurve.constant = (float)index/(float)(num);
        tsa.frameOverTime = frameOverTimeCurve;
    }
}
