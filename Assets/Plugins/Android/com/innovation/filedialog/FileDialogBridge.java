// Android 文件选择桥：启动系统文件选择器(ACTION_OPEN_DOCUMENT)，
// 选中文件后复制到本地可访问路径，结果存于静态字段供 Unity 侧轮询。
package com.innovation.filedialog;

import android.app.Activity;
import android.content.ContentResolver;
import android.content.Context;
import android.content.Intent;
import android.database.Cursor;
import android.net.Uri;
import android.os.Bundle;
import android.provider.OpenableColumns;
import android.util.Log;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;

public class FileDialogBridge
{
    private static final int REQUEST_OPEN_DOCUMENT = 0x2BAD;

    // 供 Unity 侧通过 FileDialogBridge.GetResult() 读取
    private static volatile String s_resultPath = "";
    private static volatile boolean s_resultReady = false;
    private static volatile String s_mimeType = "*/*";

    public static void OpenFile()
    {
        s_resultReady = false;
        s_resultPath = "";
        Context ctx = getUnityActivity();
        if (ctx == null) { s_resultPath = ""; s_resultReady = true; return; }
        try
        {
            Intent intent = new Intent(ctx, FileDialogActivity.class);
            intent.putExtra("mimeType", s_mimeType);
            ctx.startActivity(intent);
        }
        catch (Exception e)
        {
            Log.e("INFileDialog", "OpenFile start failed", e);
            s_resultReady = true;
        }
    }

    public static void SetMimeType(String mime)
    {
        s_mimeType = (mime == null || mime.isEmpty()) ? "*/*" : mime;
    }

    public static boolean IsReady() { return s_resultReady; }

    public static String GetResult() { return s_resultPath; }

    private static Context getUnityActivity()
    {
        try
        {
            Class<?> unityPlayer = Class.forName("com.unity3d.player.UnityPlayer");
            return (Context) unityPlayer.getMethod("currentActivity").invoke(null);
        }
        catch (Exception e)
        {
            Log.e("INFileDialog", "currentActivity failed", e);
            return null;
        }
    }

    private static String CopyUriToCache(Uri uri, Activity activity)
    {
        try
        {
            ContentResolver cr = activity.getContentResolver();
            String displayName = "selected_file_" + System.currentTimeMillis();
            Cursor cursor = cr.query(uri, null, null, null, null);
            if (cursor != null && cursor.moveToFirst())
            {
                int idx = cursor.getColumnIndex(OpenableColumns.DISPLAY_NAME);
                if (idx >= 0) displayName = cursor.getString(idx);
                cursor.close();
            }
            File out = new File(activity.getCacheDir(), displayName);
            InputStream in = cr.openInputStream(uri);
            OutputStream os = new FileOutputStream(out);
            byte[] buf = new byte[8192];
            int len;
            while ((len = in.read(buf)) > 0) os.write(buf, 0, len);
            os.close();
            in.close();
            return out.getAbsolutePath();
        }
        catch (Exception e)
        {
            Log.e("INFileDialog", "CopyUriToCache failed", e);
            return "";
        }
    }

    public static class FileDialogActivity extends Activity
    {
        @Override
        protected void onCreate(Bundle savedInstanceState)
        {
            super.onCreate(savedInstanceState);
            String mime = getIntent().getStringExtra("mimeType");
            if (mime == null || mime.isEmpty()) mime = "*/*";
            Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
            intent.addCategory(Intent.CATEGORY_OPENABLE);
            intent.setType(mime);
            try
            {
                startActivityForResult(intent, REQUEST_OPEN_DOCUMENT);
            }
            catch (Exception e)
            {
                FileDialogBridge.s_resultReady = true;
                FileDialogBridge.s_resultPath = "";
                finish();
            }
        }

        @Override
        protected void onActivityResult(int requestCode, int resultCode, Intent data)
        {
            if (resultCode == RESULT_OK && data != null && data.getData() != null)
            {
                String path = CopyUriToCache(data.getData(), this);
                FileDialogBridge.s_resultPath = path;
                FileDialogBridge.s_resultReady = true;
            }
            else
            {
                FileDialogBridge.s_resultReady = true;
                FileDialogBridge.s_resultPath = "";
            }
            finish();
        }
    }
}
