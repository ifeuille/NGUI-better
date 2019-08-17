using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if UNITY_EDITOR

#elif UNITY_IPHONE
     	   
#elif UNITY_ANDROID
class AndroidDisplayCutout
{
        
    public static bool HasNotchScreenHandle()
    {

            try
            {
                AndroidJavaObject activityJO = AndroidPlatform.CurActivity;//new AndroidJavaClass(AndroidPlatform.MAIN_CLASS_NAME).GetStatic<AndroidJavaObject>("currentActivity");//"com.unity3d.player.UnityPlayer"
                if(HasNotchScreenAndroidP(activityJO))return true; 
                if(HasNotchScreenXiaoMi(activityJO))return true; 
                if(HasNotchScreenHuaWei(activityJO))return true; 
                if(HasNotchScreenVIVO(activityJO))return true; 
                if(HasNotchScreenOPPO(activityJO))return true; 
            }
        catch(System.Exception e)
	    {
		    BaseLogSystem.internal_Info("HasNotchScreenHandle failed!!!! Exception={0}",e);
	    }
            return false;
    }
        
    //Android P判断刘海屏
    protected static bool HasNotchScreenAndroidP(AndroidJavaObject activityJO)
    {
	    if(activityJO == null) return false;
        bool ret = false;
	    try
	    {
            int SDK_INT  =  SystemPropertiesGetInt("ro.build.version.sdk",activityJO,-1);
            BaseLogSystem.internal_Info("HasNotchScreenAndroidP SDK_INT= {0}",SDK_INT);
            if(SDK_INT >= 28)
    		{
                //    ret = (GetDisplayCutoutNative(activityJO) != null);    
		        AndroidJavaObject windowsJO = activityJO.Call<AndroidJavaObject>("getWindow");
		        AndroidJavaObject decorViewJO = windowsJO.Call<AndroidJavaObject>("getDecorView");
		        AndroidJavaObject windowInsetsJO = decorViewJO.Call<AndroidJavaObject>("getRootWindowInsets");
		        if(windowInsetsJO == null)return ret;
		        ret = (null != windowInsetsJO.Call<AndroidJavaObject>("getDisplayCutout")); 
                //Main.errorString += string.Format("HasNotchScreenAndroidP has notch = {0}\n",ret);
                //if(Screen.safeArea.x != Screen.x || Screen.safeArea.width != Screen.width)
                //{
                //    ret = true;
                //}
                BaseLogSystem.internal_Info("HasNotchScreenAndroidP hasNotchInScreen={0}",ret);
            }              
	    }
	    catch(System.Exception e)
	    {
		    BaseLogSystem.internal_Info("HasNotchScreenAndroidP failed!!!! Exception={0}",e);
	    }
	    return ret;
    }
        
    //小米判断刘海屏
    protected static bool HasNotchScreenXiaoMi(AndroidJavaObject activityJO)
    {
        bool ret = (SystemPropertiesGetInt("ro.miui.notch",activityJO,-1) == 1);
        BaseLogSystem.internal_Info("HasNotchScreenXiaoMi hasNotchInScreen={0}",ret);
        return ret;
    }

    //华为判断刘海屏
    protected static bool HasNotchScreenHuaWei(AndroidJavaObject activityJO)
    {
        //http://mini.eastday.com/bdmip/180411011257629.html
        bool ret = false;
        try
        {
            BaseLogSystem.internal_Info("HasNotchScreenHuaWei hasNotchInScreen={0}",ret);
            AndroidJavaClass jo = new AndroidJavaClass("com.huawei.android.util.HwNotchSizeUtil");
            ret = jo.CallStatic<bool>("hasNotchInScreen");
        }
        catch(System.Exception e)
	    {
		    BaseLogSystem.internal_Info("HasNotchScreenHuaWei failed!!!! Exception={0}",e);
	    }
        return ret;
    }

    //VIVO刘海屏判断
    protected static bool HasNotchScreenVIVO(AndroidJavaObject activityJO)
    {
        bool ret = false;
        try
        {
            //int VIVO_NOTCH = 0x00000020;//是否有刘海
            //int VIVO_FILLET = 0x00000008;//是否有圆角
            //IntPtr clsPtr = AndroidJNI.FindClass("android.util.FtFeature");
            //IntPtr funcID = AndroidJNIHelper.GetMethodID(clsPtr, "isFeatureSupport","()Z");                
            //ret = AndroidJNI.CallBooleanMethod(clsPtr,funcID,new []{new jvalue() { i=VIVO_NOTCH }});
            AndroidJavaClass jo = new AndroidJavaClass("android.util.FtFeature");
            ret = jo.CallStatic<bool>("isFeatureSupport",0x00000020);
            BaseLogSystem.internal_Info("HasNotchScreenVIVO IsSupportNotchScreen={0}",ret);
        }
        catch(System.Exception e)
	    {
		    BaseLogSystem.internal_Info("HasNotchScreenVIVO failed!!!! Exception={0}",e);
	    }
        return ret;
    }

    //OPPO刘海屏判断
    protected static bool HasNotchScreenOPPO(AndroidJavaObject activityJO)
    {
        bool ret = false;
        try
        {
            AndroidJavaObject packageJO = activityJO.Call<AndroidJavaObject>("getPackageManager");
            ret = packageJO.Call<bool>("hasSystemFeature","com.oppo.feature.screen.heteromorphism");
            BaseLogSystem.internal_Info("HasNotchScreenOPPO IsSupportNotchScreen={0}",ret);
        }
        catch(System.Exception e)
	    {
		    BaseLogSystem.internal_Info("HasNotchScreenOPPO failed!!!! Exception={0}",e);
	    }
        return ret;
    }
        
    protected static AndroidJavaObject GetDisplayCutoutNative(AndroidJavaObject activityJO)
    {
	    if(activityJO == null)return null;
	    try
	    {
		    AndroidJavaObject windowsJO = activityJO.Call<AndroidJavaObject>("getWindow");
		    AndroidJavaObject decorViewJO = windowsJO.Call<AndroidJavaObject>("getDecorView");
		    AndroidJavaObject windowInsetsJO = decorViewJO.Call<AndroidJavaObject>("getRootWindowInsets");
		    if(windowInsetsJO == null)return null;
		    return windowInsetsJO.Call<AndroidJavaObject>("getDisplayCutout");                
	    }
	    catch(System.Exception e)
	    {
		    BaseLogSystem.internal_Info("GetDisplayCutoutNative failed!!!! Exception={0}",e);
	    }
	    return null;
    }
        
    protected static int SystemPropertiesGetInt(string key,AndroidJavaObject activityJO,int def)
    {
        if(activityJO == null)return -1;
        if(activityJO == null)return -1;
	    try
	    {
		    //IntPtr clsPtr = AndroidJNI.FindClass("android.os.SystemProperties");
		    //IntPtr funcIDGetInt = AndroidJNIHelper.GetMethodID(clsPtr, "getInt","(Ljava/lang/String;I)I");//getInt(String key, int def)                
		    //return AndroidJNI.CallStaticIntMethod(clsPtr,funcIDGetInt,
		    //                AndroidJNI.NewStringUTF(key),def);  
                AndroidJavaClass jo = new AndroidJavaClass("android.os.SystemProperties");
                return jo.CallStatic<int>("getInt",key,-1);
	    }
	    catch(System.Exception e)
	    {
		    BaseLogSystem.internal_Info("SystemPropertiesGetInt failed!!!! Exception={0}",e);
	    }
        return -1;
    }

    //基于Screen.orientation == ScreenOrientation.LandscapeLeft 也就是横屏,home在右边的情况
    protected enum EnumSafeInsetArg
    {
        eLeft = 0,eRight = 1
        //,eTop,eBottom
    }
        
    //获得左侧不安全区像素大小
    public static int GetSafeInsetLeftOfLandscapeLeft(int def)
    {            
        AndroidJavaObject activityJO = AndroidPlatform.CurActivity;//new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");                
        EnumSafeInsetArg eArg = EnumSafeInsetArg.eLeft;            
        int ret = def;
        if( GetSafeInsetAndroidP(activityJO,ref ret,eArg))
        {
            return ret;
        }
        if(HasNotchScreenOPPO(activityJO))
        {
            if(GetSafeInsetOPPO(activityJO,ref ret,eArg))
                return ret;
        }
        if(HasNotchScreenVIVO(activityJO))
        {
            if(GetSafeInsetVIVO(activityJO,ref ret,eArg))
                return ret;
        }
        if(HasNotchScreenHuaWei(activityJO))
        {
            if(GetSafeInsetHuawei(activityJO,ref ret,eArg))
                return ret;
        }
        if(HasNotchScreenXiaoMi(activityJO))
        {
            if(GetSafeInsetXiaomi(activityJO,ref ret,eArg))
                return ret;
        }
        return ret;
    }

    //获得右侧不安全区像素大小
    public static int GetSafeInsetRightOfLandscapeLeft(int def)
    {
        AndroidJavaObject activityJO = AndroidPlatform.CurActivity;//new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");                
        EnumSafeInsetArg eArg = EnumSafeInsetArg.eRight;            
        int ret = def;
        if(GetSafeInsetAndroidP(activityJO,ref ret,eArg))
        {
            return ret;
        }
        if(HasNotchScreenOPPO(activityJO))
        {
            if(GetSafeInsetOPPO(activityJO,ref ret,eArg))
                return ret;
        }
        if(HasNotchScreenVIVO(activityJO))
        {
            if(GetSafeInsetVIVO(activityJO,ref ret,eArg))
                return ret;
        }
        if(HasNotchScreenHuaWei(activityJO))
        {
            if(GetSafeInsetHuawei(activityJO,ref ret,eArg))
                return ret;
        }
        if(HasNotchScreenXiaoMi(activityJO))
        {
            if(GetSafeInsetXiaomi(activityJO,ref ret,eArg))
                return ret;
        }
        return ret;
    }


    //android p
    protected static bool GetSafeInsetAndroidP(AndroidJavaObject activityJO,ref int outValue,EnumSafeInsetArg eArg)
    {
        try
        {
            int SDK_INT  =  SystemPropertiesGetInt("ro.build.version.sdk",activityJO,-1);
            if(SDK_INT >= 28)
    		{
                int left = (int)Screen.safeArea.x;//对称,两边用一样的值
                BaseLogSystem.internal_Info("GetSafeInsetAndroidP Screen.safeArea.x={0}",Screen.safeArea.x);
                AndroidJavaObject windowsJO = activityJO.Call<AndroidJavaObject>("getWindow");
		        AndroidJavaObject decorViewJO = windowsJO.Call<AndroidJavaObject>("getDecorView");
		        AndroidJavaObject windowInsetsJO = decorViewJO.Call<AndroidJavaObject>("getRootWindowInsets");
		        if(windowInsetsJO == null)
                {
                    BaseLogSystem.internal_Info("GetSafeInsetAndroidP false");
                    return false;		
                }
                //AndroidJavaObject displayCutout = windowInsetsJO.Call<AndroidJavaObject>("getDisplayCutout");
                //left = displayCutout.Call<int>("SafeInsetLeft");
                left = AndroidPlatform.MainActivityClass.CallStatic<int>("GetAndroidPSafeInsetTop");
                BaseLogSystem.internal_Info("GetSafeInsetAndroidP GetAndroidPSafeInsetTop()={0}",left);
                if(Screen.orientation == ScreenOrientation.LandscapeRight)
                {
                    left = AndroidPlatform.MainActivityClass.CallStatic<int>("GetAndroidPSafeInsetRight");
                    BaseLogSystem.internal_Info("GetSafeInsetAndroidP GetAndroidPSafeInsetRight()={0}",left);
                }
                else
                {
                    left = AndroidPlatform.MainActivityClass.CallStatic<int>("GetAndroidPSafeInsetLeft");
                    BaseLogSystem.internal_Info("GetSafeInsetAndroidP GetAndroidPSafeInsetLeft()={0}",left);
                }
                    
                BaseLogSystem.internal_Info("GetSafeInsetAndroidP GetAndroidPSafeInsetLeft()={0}",left);
                outValue = left;
                return true;
            }
        }
	    catch(System.Exception e)
	    {
            //Main.errorString += string.Format("GetSafeInsetAndroidP failed!! Exception={0}\n",e);
		    BaseLogSystem.internal_Info("GetSafeInsetAndroidP failed!!!! Exception={0}",e);
	    }
        return false;
    }
    //xiaomi
    protected static bool GetSafeInsetXiaomi(AndroidJavaObject activityJO,ref int outValue,EnumSafeInsetArg eArg)
    {
        //https://www.jianshu.com/p/7a934313637e
        try
        {
            //AndroidJavaObject resourceJO = activityJO.Call<AndroidJavaObject>("getResources");
            //int rsID = resourceJO.Call<int>("getIdentifier","notch_height", "dimen", "android");
            //if (rsID > 0) 
            //{
            //    outValue = resourceJO.Call<int>("getDimensionPixelSize",rsID);
            //    //Main.errorString += string.Format("GetSafeInsetXiaomi notch height={0}\n",outValue);
            //    BaseLogSystem.internal_Info("GetSafeInsetXiaomi notch height={0}",outValue);
            //    return true;
            //}
            bool ret = GetSafeInsetByDimen(activityJO,ref outValue);
            //Main.errorString += string.Format("GetSafeInsetXiaomi notch height={0}\n",outValue);
            BaseLogSystem.internal_Info("GetSafeInsetXiaomi notch height={0}",outValue);
            return ret;
        }
	    catch(System.Exception e)
	    {
            //Main.errorString += string.Format("GetSafeInsetXiaomi failed!! Exception={0}\n",e);
		    BaseLogSystem.internal_Info("GetSafeInsetXiaomi failed!!!! Exception={0}",e);
	    }
        return false;
    }

    //huawei获取刘海尺寸：width、height int[0]值为刘海宽度 int[1]值为刘海高度。
    protected static bool GetSafeInsetHuawei(AndroidJavaObject activityJO,ref int outValue,EnumSafeInsetArg eArg)
    {
        try
        {
            AndroidJavaClass jo = new AndroidJavaClass("com.huawei.android.util.HwNotchSizeUtil");           
            int[] info = jo.CallStatic<int[]>("getNotchSize");
            outValue = info[1];
            //Main.errorString += string.Format("GetSafeInsetHuawei notch height={0}",outValue);
            BaseLogSystem.internal_Info("GetSafeInsetHuawei notch height={0}",outValue);
            return true;
        }
	    catch(System.Exception e)
	    {
            //Main.errorString += string.Format("GetSafeInsetHuawei failed!! Exception={0}\n",e);
		    BaseLogSystem.internal_Info("GetSafeInsetHuawei failed!!!! Exception={0}",e);
	    }
        return false;
    }
    //oppo
    protected static bool GetSafeInsetOPPO(AndroidJavaObject activityJO,ref int outValue,EnumSafeInsetArg eArg)
    {
        try
        {
            bool ret = GetSafeInsetByDimen(activityJO,ref outValue);
            if(!ret)
                outValue = 80;
            //Main.errorString += string.Format("GetSafeInsetOPPO notch height={0}\n",outValue);
            return true;
        }
	    catch(System.Exception e)
	    {
            //Main.errorString += string.Format("GetSafeInsetOPPO failed!! Exception={0}\n",e);
		    BaseLogSystem.internal_Info("GetSafeInsetOPPO failed!!!! Exception={0}",e);
	    }
        return false;
    }

    //vivo
    protected static bool GetSafeInsetVIVO(AndroidJavaObject activityJO,ref int outValue,EnumSafeInsetArg eArg)
    {
        try
        {
            bool ret = GetSafeInsetByDimen(activityJO,ref outValue);
            if(!ret)
                outValue = dip2px(activityJO,27);
            //Main.errorString += string.Format("GetSafeInsetVIVO notch height={0}\n",outValue);
            return true;
        }
	    catch(System.Exception e)
	    {
            //Main.errorString += string.Format("GetSafeInsetVIVO failed!! Exception={0}\n",e);
		    BaseLogSystem.internal_Info("GetSafeInsetVIVO failed!!!! Exception={0}",e);
	    }
        return false;
    }

    public static int dip2px(AndroidJavaObject activityJO, float dpValue) 
    {
        try
        {
            AndroidJavaObject resourceJO = activityJO.Call<AndroidJavaObject>("getResources");
            AndroidJavaObject displayMetricsJO = resourceJO.Call<AndroidJavaObject>("getDisplayMetrics");
            float scale = displayMetricsJO.Get<float>("density");
            return (int)(dpValue*scale + 0.5f);                
        }
	    catch(System.Exception e)
	    {
            //Main.errorString += string.Format("dip2px failed!! Exception={0}\n",e);
		    BaseLogSystem.internal_Info("dip2px failed!!!! Exception={0}",e);
	    }
        return (int)dpValue;
    }

    //通过顶部状态栏高度替代刘海高度
    protected static bool GetSafeInsetByDimen(AndroidJavaObject activityJO,ref int outValue)
    {
        //https://www.jianshu.com/p/7a934313637e
        try
        {
            AndroidJavaObject resourceJO = activityJO.Call<AndroidJavaObject>("getResources");
            int rsID = resourceJO.Call<int>("getIdentifier","notch_height", "dimen", "android");
            if (rsID > 0) 
            {
                outValue = resourceJO.Call<int>("getDimensionPixelSize",rsID);
                //Main.errorString += string.Format("GetSafeInsetByDimen notch height={0}\n",outValue);
                BaseLogSystem.internal_Info("GetSafeInsetByDimen notch height={0}",outValue);
                return true;
            }
        }
	    catch(System.Exception e)
	    {
            //Main.errorString += string.Format("GetSafeInsetByDimen failed!! Exception={0}\n",e);
		    BaseLogSystem.internal_Info("GetSafeInsetByDimen failed!!!! Exception={0}",e);
	    }
        return false;
    }

}
#endif
