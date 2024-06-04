using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Windows.WebCam;

public class SmartCrackMeasurement : MonoBehaviour
{
    public string text;
    PhotoCapture photoCaptureObject = null;
    Texture2D targetTexture = null;
    public float DTC;
    public float threshold1;
    public float threshold2;
    public float MaxThicknessIn_mm;
    public float p_min;
    public int mbatch ;
    public int nbatch;
    public int row_max = 3;
    public float Length_mm;
    public float AverageThicknessIn_mm;
    public float Area;
    public int val;
    public Material OutputMaterial;
    public Texture2D StaticTexture;
    public double ProcessingTime;
    int n1, n2, m1, m2, n_batch1, n_batch2, m_batch1, m_batch2;
    public string EvaluationResult;
    CameraParameters cameraParameters = new CameraParameters();
    Renderer rndr;
    Texture texture = null;
    WebCamTexture webcamTexture;
    public float p, l;
    void Start()
    {
        rndr = GetComponent<Renderer>();
        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);

        // Create a PhotoCapture object
        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject)
        {
            photoCaptureObject = captureObject;
            cameraParameters.hologramOpacity = 0.5f;
            cameraParameters.cameraResolutionWidth = cameraResolution.width;
            cameraParameters.cameraResolutionHeight = cameraResolution.height;
            cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;

            // Activate the camera
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result)
            {
                // Take a picture
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            });
        });
    }
    private void Update()
    {
        // Take a picture
        photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
    }
    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        DateTime start = DateTime.Now;
        // Copy the raw image data into our target texture
        photoCaptureFrame.UploadImageDataToTexture(targetTexture);
        // Copy the raw image data into our target texture
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, float.PositiveInfinity, LayerMask.GetMask("Spatial Awareness")))
        {
            float distanceToCrack = Vector3.Distance(hit.point, Camera.main.transform.position);
            DTC = distanceToCrack;
        }
        // duplicate the original texture and assign to the material
        // tint each mip level
        var cols = targetTexture.GetPixels32(0);
        float[] values = new float[cols.Length];
        float[] Gx = new float[cols.Length];
        float[] Gy = new float[cols.Length];
        float[] G = new float[cols.Length];
        float[] teta = new float[cols.Length];
        n1 = targetTexture.height / 10 * ((Mathf.FloorToInt((10 - p) / 2))) + 0;
        n2 = targetTexture.height / 10 * (10 - (Mathf.FloorToInt((10 - p) / 2))) - 0;
        m1 = targetTexture.width / 10 * ((Mathf.FloorToInt((10 - l) / 2))) + 0;
        m2 = targetTexture.width / 10 * (10 - (Mathf.FloorToInt((10 - l) / 2))) - 0;
        n_batch1 = (int)Mathf.Floor(nbatch * n1 / targetTexture.height);
        n_batch2 = (int)Mathf.Floor(nbatch * n2 / targetTexture.height);
        m_batch1 = (int)Mathf.Floor(mbatch * m1 / targetTexture.width);
        m_batch2 = (int)Mathf.Floor(mbatch * m2 / targetTexture.width);
        float[] k2 = new float[n2 - n1 + 1400];
        float[] k7 = new float[n2 - n1 + 1400];
        float[] k9 = new float[n2 - n1 + 1400];
        int[] s1 = new int[n2 - n1 + 1400];
        int[] s2 = new int[n2 - n1 + 1400];
        int[] s4 = new int[n2 - n1 + 1400];
        int px = (int)(Mathf.Floor(targetTexture.width / mbatch));
        int py = (int)(Mathf.Floor(targetTexture.height / nbatch));
        int i, j = m_batch1;
        int b = 1;
        float sum = 0, max = 0;
        float[] PP = new float[nbatch * mbatch + 400];
        bool endLoop = false;
        if (DTC > 1.5)
        {
            threshold1 = 11000;
            threshold2 = 9000;
        }
        else if (DTC > 0.5 && DTC < 0.9)

        {
            threshold1 = 25000;
            threshold2 = 20000;
        }
        else if (DTC < 0.5)
        {
            threshold1 = 40000;
            threshold2 = 30000;
        }
        else
        {
            threshold1 = 20000;
            threshold2 = 17000;
        }
        // Loops over batches
        for (i = n_batch1; i < n_batch1 + row_max; ++i)
        {
            for (j = m_batch1; j <= m_batch2; ++j)
            {
                // Greying 
                for (int k = py * (i - 1) - 1; k <= py * i + 1; ++k)
                {
                    cols[k * targetTexture.width + px * (j - 1) - 1] = Color.blue;
                    cols[k * targetTexture.width + px * j + 1] = Color.blue;
                    for (int l = px * (j - 1) - 1; l <= px * j + 1; ++l)
                    {
                        values[k * targetTexture.width + l] = 0.587f * cols[k * targetTexture.width + l].g + 0.299f * cols[k * targetTexture.width + l].r +
                            0.114f * cols[k * targetTexture.width + l].b;
                    }
                }

                // Smoothing 
                for (int k = py * (i - 1); k <= py * i - 1; ++k)
                {
                    for (int l = px * (j - 1); l <= px * j - 1; ++l)
                    {
                        float[] val = new float[8] { values[(k - 1) * targetTexture.width + l], values[(k + 1) * targetTexture.width + l], values[k * targetTexture.width + l - 1], values[k * targetTexture.width + l + 1], values[(k - 1) * targetTexture.width + l - 1], values[(k - 1) * targetTexture.width + l + 1], values[(k + 1) * targetTexture.width + l - 1], values[(k + 1) * targetTexture.width + l + 1] };
                        Array.Sort(val);
                        values[k * targetTexture.width + l] = (val[3] + val[4]) / 2;
                    }
                }
                // Gradint Calculations 
                for (int k = py * (i - 1); k <= py * i - 1; ++k)
                {
                    for (int l = px * (j - 1); l <= px * j - 1; ++l)
                    {
                        Gx[k * targetTexture.width + l] = -1 * values[k * targetTexture.width + l - targetTexture.width - 1] + values[k * targetTexture.width + l - targetTexture.width + 1]
                            - 2 * values[k * targetTexture.width + l - 1] + 2 * values[k * targetTexture.width + l + 1] - 1 * values[k * targetTexture.width + l + targetTexture.width - 1]
                            + values[k * targetTexture.width + l + targetTexture.width + 1];
                        Gy[k * targetTexture.width + l] = -1 * values[k * targetTexture.width + l - targetTexture.width - 1] - 1 * values[k * targetTexture.width + l - targetTexture.width + 1]
                            - 2 * values[k * targetTexture.width + l - targetTexture.width] + 2 * values[k * targetTexture.width + l + targetTexture.width]
                            + 1 * values[k * targetTexture.width + l + targetTexture.width - 1] + values[k * targetTexture.width + l + targetTexture.width + 1];
                        G[k * targetTexture.width + l] = Mathf.Pow(Gx[k * targetTexture.width + l], 2) + Mathf.Pow(Gy[k * targetTexture.width + l], 2);
                        teta[k * targetTexture.width + l] = Mathf.Atan(Gy[k * targetTexture.width + l] / Gx[k * targetTexture.width + l]);
                    }
                }

                // Thresholding
                for (int k = py * (i - 1); k <= py * i - 1; ++k)
                {
                    for (int l = px * (j - 1); l <= px * j - 1; ++l)
                    {
                        if (G[k * targetTexture.width + l] >= threshold1)
                        {
                            PP[i * mbatch + j]++;
                        }
                    }
                }
                if (endLoop == true && j>m_batch1+1)
                {
                    break;
                }
                if (PP[i * mbatch + j] > py * p_min)
                {
                    endLoop = true;
                }
            }
            if (endLoop == true)
            {
                break;
            }
        }
        if (j != m_batch2 + 1)
        {
            j--;
            for (int i2 = i; i2 <= n_batch2; ++i2)
            {
                // Measurement
                for (int k = py * (i2 - 1); k <= py * i2 - 1; ++k)
                {
                    for (int pixel = px * (j - 2) + 1; pixel <= px * (j + 1) - 2; ++pixel)
                    {
                        if (G[k * targetTexture.width + pixel] > threshold1)
                        {
                            if ((G[k * targetTexture.width + pixel + 1] > threshold2 && G[k * targetTexture.width + pixel - 1] > threshold2) || (G[(k + 1) * targetTexture.width + pixel] > threshold2 && G[(k - 1) * targetTexture.width + pixel] > threshold2) || (G[(k + 1) * targetTexture.width + pixel - 1] > threshold2 & G[(k - 1) * targetTexture.width + pixel + 1] > threshold2) || (G[(k + 1) * targetTexture.width + pixel + 1] > threshold2 && G[(k - 1) * targetTexture.width + pixel - 1] > threshold2))
                            {
                                s1[k + py - py * i] = pixel;
                                ++pixel;
                                while (G[k * targetTexture.width + pixel] > threshold1)
                                {
                                    ++pixel;
                                }
                                s2[k + py - py * i] = pixel;
                                while (G[k * targetTexture.width + pixel] < threshold1 && (pixel - s2[k + py - py * i]) < 30)
                                {
                                    ++pixel;
                                }
                                if (pixel - s2[k + py - py * i] < 30 && pixel <= px * (j + 1) - 2)
                                {
                                    while (G[k * targetTexture.width + pixel] > threshold1)
                                    {
                                        ++pixel;
                                    }
                                    s4[k + py - py * i] = pixel - 1;
                                    k7[k + py - py * i] = Mathf.Cos(teta[k * targetTexture.width + s1[k + py - py * i]]);
                                    k9[k + py - py * i] = Mathf.Cos(teta[k * targetTexture.width + pixel - 1]);
                                    while (G[k * targetTexture.width + pixel] < threshold1 && (pixel - s2[k + py - py * i]) < 30)
                                    {
                                        ++pixel;
                                    }
                                    if (pixel - s2[k + py - py * i] < 30)
                                    {
                                        while (G[k * targetTexture.width + pixel] > threshold1)
                                        {
                                            ++pixel;
                                        }
                                        s4[k + py - py * i] = pixel - 1;
                                        k7[k + py - py * i] = Mathf.Cos(teta[k * targetTexture.width + s1[k + py - py * i]]);
                                        k9[k + py - py * i] = Mathf.Cos(teta[k * targetTexture.width + pixel - 1]);
                                    }
                                    if (s4[k + py - py * i]- s2[k + py - py * i]!=30) 
                                    {
                                        for (int d = s2[k + py - py * i]; d <= s4[k + py - py * i]; ++d)
                                        {
                                            cols[k * targetTexture.width + d] = Color.red;
                                        }
                                    }
                                    else
                                    {
                                        s4[k + py - py * i] = s2[k + py - py * i];
                                        k7[k + py - py * i] = 0;
                                        k9[k + py - py * i] = 0;
                                    }
                                    k2[k + py - py * i] = (s4[k + py - py * i] - s2[k + py - py * i]);
                                    break;
                                }
                            }
                        }
                    }
                }
                if (PP[i2 * mbatch + j - 1] > PP[i2 * mbatch + j] && PP[i2 * mbatch + j - 1] >= PP[i2 * mbatch + j + 1])
                {
                    if (j > 2)
                    {
                        --j;

                    }
                }
                else if (PP[i2 * mbatch + j + 1] > PP[i2 * mbatch + j] && PP[i2 * mbatch + j + 1] > PP[i2 * mbatch + j - 1])
                {
                    if (j<mbatch-1)
                    {
                        ++j;
                    }
                }
                for (int index = py * (i2 - 1) * targetTexture.width + px * (j - 2);
                    index <= py * (i2 - 1) * targetTexture.width + px * (j + 1) - 1; index++)
                {
                    cols[index] = Color.green;
                }
                for (int index = (py * i2 - 1) * targetTexture.width + px * (j - 2);
                 index <= (py * i2 - 1) * targetTexture.width + px * (j + 1) - 1; index++)
                {
                    cols[index] = Color.green;
                }
                for (int k = py * i2 - 1; k <= py * (i2 + 1) + 1; ++k)
                {
                    cols[k * targetTexture.width + px * (j - 2) + 1] = Color.green;
                    cols[k * targetTexture.width + px * (j + 1) - 2] = Color.green;
                    for (int l = px * (j - 2) - 1; l <= px * (j + 1) + 1; ++l)
                    {
                        values[k * targetTexture.width + l] = 0.587f * cols[k * targetTexture.width + l].g + 0.299f * cols[k * targetTexture.width + l].r +
                            0.114f * cols[k * targetTexture.width + l].b;
                    }
                }
                for (int k = py * i2; k <= py * (i2 + 1) - 1; ++k)
                {
                    for (int l = px * (j - 2); l <= px * (j + 1) - 1; ++l)
                    {
                        float[] val = new float[8] { values[(k - 1) * targetTexture.width + l], values[(k + 1) * targetTexture.width + l], values[k * targetTexture.width + l - 1], values[k * targetTexture.width + l + 1], values[(k - 1) * targetTexture.width + l - 1], values[(k - 1) * targetTexture.width + l + 1], values[(k + 1) * targetTexture.width + l - 1], values[(k + 1) * targetTexture.width + l + 1] };
                        Array.Sort(val);
                        values[k * targetTexture.width + l] = (val[3] + val[4]) / 2;
                    }
                }
                for (int k = py * i2; k <= py * (i2 + 1) - 1; ++k)
                {
                    for (int l1 = px * (j - 2); l1 <= px * (j - 1) - 1; ++l1)
                    {
                        Gx[k * targetTexture.width + l1] = -1 * values[k * targetTexture.width + l1 - targetTexture.width - 1] +
                        values[k * targetTexture.width + l1 - targetTexture.width + 1]
                        - 2 * values[k * targetTexture.width + l1 - 1] + 2 * values[k * targetTexture.width + l1 + 1] -
                         1 * values[k * targetTexture.width + l1 + targetTexture.width - 1]
                        + values[k * targetTexture.width + l1 + targetTexture.width + 1];
                        Gy[k * targetTexture.width + l1] = -1 * values[k * targetTexture.width + l1 - targetTexture.width - 1] -
                        1 * values[k * targetTexture.width + l1 - targetTexture.width + 1]
                        - 2 * values[k * targetTexture.width + l1 - targetTexture.width] + 2 * values[k * targetTexture.width + l1 + targetTexture.width]
                        + 1 * values[k * targetTexture.width + l1 + targetTexture.width - 1] + values[k * targetTexture.width + l1 + targetTexture.width + 1];
                        G[k * targetTexture.width + l1] = Mathf.Pow(Gx[k * targetTexture.width + l1], 2) + Mathf.Pow(Gy[k * targetTexture.width + l1], 2);
                        teta[k * targetTexture.width + l1] = Mathf.Atan(Gy[k * targetTexture.width + l1] / Gx[k * targetTexture.width + l1]);
                        if (G[k * targetTexture.width + l1] >= threshold1)
                        {
                            PP[(i2 + 1) * mbatch + j - 1]++;
                        }
                    }
                    for (int l2 = px * (j - 1); l2 <= px * j - 1; ++l2)
                    {
                        Gx[k * targetTexture.width + l2] = -1 * values[k * targetTexture.width + l2 - targetTexture.width - 1] +
                        values[k * targetTexture.width + l2 - targetTexture.width + 1]
                        - 2 * values[k * targetTexture.width + l2 - 1] + 2 * values[k * targetTexture.width + l2 + 1] -
                        1 * values[k * targetTexture.width + l2 + targetTexture.width - 1]
                        + values[k * targetTexture.width + l2 + targetTexture.width + 1];
                        Gy[k * targetTexture.width + l2] = -1 * values[k * targetTexture.width + l2 - targetTexture.width - 1] -
                        1 * values[k * targetTexture.width + l2 - targetTexture.width + 1]
                        - 2 * values[k * targetTexture.width + l2 - targetTexture.width] + 2 * values[k * targetTexture.width + l2 + targetTexture.width]
                        + 1 * values[k * targetTexture.width + l2 + targetTexture.width - 1] + values[k * targetTexture.width + l2 + targetTexture.width + 1];
                        G[k * targetTexture.width + l2] = Mathf.Pow(Gx[k * targetTexture.width + l2], 2) + Mathf.Pow(Gy[k * targetTexture.width + l2], 2);
                        teta[k * targetTexture.width + l2] = Mathf.Atan(Gy[k * targetTexture.width + l2] / Gx[k * targetTexture.width + l2]);
                        if (G[k * targetTexture.width + l2] >= threshold1)
                        {
                            PP[(i2 + 1) * mbatch + j]++;
                        }
                    }
                    for (int l3 = px * j; l3 <= px * (j + 1); ++l3)
                    {
                        Gx[k * targetTexture.width + l3] = -1 * values[k * targetTexture.width + l3 - targetTexture.width - 1] +
                        values[k * targetTexture.width + l3 - targetTexture.width + 1]
                        - 2 * values[k * targetTexture.width + l3 - 1] + 2 * values[k * targetTexture.width + l3 + 1] -
                        1 * values[k * targetTexture.width + l3 + targetTexture.width - 1]
                        + values[k * targetTexture.width + l3 + targetTexture.width + 1];
                        Gy[k * targetTexture.width + l3] = -1 * values[k * targetTexture.width + l3 - targetTexture.width - 1] -
                        1 * values[k * targetTexture.width + l3 - targetTexture.width + 1]
                        - 2 * values[k * targetTexture.width + l3 - targetTexture.width] + 2 * values[k * targetTexture.width + l3 + targetTexture.width]
                        + 1 * values[k * targetTexture.width + l3 + targetTexture.width - 1] + values[k * targetTexture.width + l3 + targetTexture.width + 1];
                        G[k * targetTexture.width + l3] = Mathf.Pow(Gx[k * targetTexture.width + l3], 2) + Mathf.Pow(Gy[k * targetTexture.width + l3], 2);
                        teta[k * targetTexture.width + l3] = Mathf.Atan(Gy[k * targetTexture.width + l3] / Gx[k * targetTexture.width + l3]);
                        if (G[k * targetTexture.width + l3] >= threshold1)
                        {
                            PP[(i2 + 1) * mbatch + j + 1]++;
                        }
                    }
                }
                if (Mathf.Max(PP[(i2 + 1) * mbatch + j - 1], PP[(i2 + 1) * mbatch + j], PP[(i2 + 1) * mbatch + j + 1]) >= py * p_min/3f)
                {
                    b = 1;
                }
                else
                {
                    ++b;
                    if (b >= row_max)
                    {
                        break;
                    }
                }
            }
            int n = 0;
            float ll = 0.0f;
            float area = 0;
            for (int x = 0; x < n2 - n1; x++)
            {
                if (k2[x] < 34 && k2[x] > 2)
                {
                    n++;
                    sum += k2[x] * (k7[x] + k9[x]) / 2;
                    area += k2[x];
                    ll += 1.0f / ((k7[x] + k9[x]) / 2);
                    if (k2[x] * (k7[x] + k9[x]) / 2 > max)
                    {
                        max = k2[x] * (k7[x] + k9[x]) / 2;
                    }
                }
            }
            if (n != 0)
            {
                AverageThicknessIn_mm = ((sum / (n)) * 0.3f) * Mathf.Pow((DTC - 0.07f), 0.82f);
                MaxThicknessIn_mm = (max * 0.3f) * Mathf.Pow((DTC - 0.07f), 0.82f);
                Length_mm = (n * 100 / (n2 - n1));
                Area = (DTC);
            }
        }
        targetTexture.SetPixels32(cols);
        targetTexture.Apply();
        rndr.material.mainTexture = targetTexture;
        DateTime end = DateTime.Now;
        TimeSpan ts = end - start;
        ProcessingTime = ts.TotalMilliseconds;
        //MaxThicknessIn_mm = (float)ProcessingTime / 1000;
    }
}