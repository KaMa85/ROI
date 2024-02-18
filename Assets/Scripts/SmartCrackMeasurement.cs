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
    public float threshold1= 100000;
    public float threshold2= 75000;
    public float MaxThicknessIn_mm;
    public float p_min=2.0f;
    public int mbatch;
    public int nbatch;
    public int row_max=3;
    public float Length_mm;
    public float AverageThicknessIn_mm;
    public float Area;
    public int val;
    public Material OutputMaterial;
    public Texture2D StaticTexture;
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
        n1 = (int)(targetTexture.height / 10 * Mathf.Floor((10 - p) / 2)) + 00;
        n2 = (int)(targetTexture.height / 10 * (10 - Mathf.Floor((10 - p) / 2))) - 00;
        m1 = (int)(targetTexture.width / 10 * Mathf.Floor((10 - l) / 2)) + 00;
        m2 = (int)(targetTexture.width / 10 * (10 - Mathf.Floor((10 - l) / 2))) - 00;
        n_batch1 = (int)Mathf.Floor(nbatch * n1 / targetTexture.height);
        n_batch2 = (int)Mathf.Floor(nbatch * n2 / targetTexture.height);
        m_batch1 = (int)Mathf.Floor(mbatch * m1 / targetTexture.width);
        m_batch2 = (int)Mathf.Floor(mbatch * m2 / targetTexture.width);
        float[] k2 = new float[n2 - n1 + 400];
        float[] k7 = new float[n2 - n1 + 400];
        float[] k9 = new float[n2 - n1 + 400];
        int[] s1 = new int[n2 - n1 + 400];
        int[] s2 = new int[n2 - n1 + 400];
        int[] s4 = new int[n2 - n1 + 400];
        int px = (int)(Mathf.Floor(targetTexture.width / mbatch));
        int py = (int)(Mathf.Floor(targetTexture.height / nbatch));
        int i, j = m_batch1;
        int i_start;
        int b = 1;
        float sum = 0, max = 0;
        float[] PP = new float[mbatch * nbatch + 5];
        bool endLoop = false;
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
                if (endLoop == true)
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
            i_start = i;
            for (i = i_start; i <= n_batch2; ++i)
            {
                // Measurement
                for (int k = py * (i - 1); k <= py * i - 1; ++k)
                {
                    for (int pixel = px * (j - 2) + 1; pixel <= px * (j + 1) - 2; ++pixel)
                    {
                        if (G[k * targetTexture.width + pixel] > threshold1)
                        {
                            if ((G[k * targetTexture.width + pixel + 1] > threshold2 && G[k * targetTexture.width + pixel - 1] > threshold2) || (G[(k + 1) * targetTexture.width + pixel] > threshold2 && G[(k - 1) * targetTexture.width + pixel] > threshold2) || (G[(k + 1) * targetTexture.width + pixel - 1] > threshold2 & G[(k - 1) * targetTexture.width + pixel + 1] > threshold2) || (G[(k + 1) * targetTexture.width + pixel + 1] > threshold2 && G[(k - 1) * targetTexture.width + pixel - 1] > threshold2))
                            {
                                s1[k - 1 + py - py * i_start] = pixel;
                                ++pixel;
                                while (G[k * targetTexture.width + pixel] > threshold1)
                                {
                                    ++pixel;
                                }
                                s2[k - 1 + py - py * i_start] = pixel;
                                while (G[k * targetTexture.width + pixel] < threshold1 && (pixel - s2[k - 1 + py - py * i_start]) < 25)
                                {
                                    ++pixel;
                                }
                                if (pixel - s2[k - 1 + py - py * i_start] < 25 && pixel <= px * (j + 1) - 2)
                                {
                                    while (G[k * targetTexture.width + pixel] > threshold1)
                                    {
                                        ++pixel;
                                    }
                                    s4[k - 1 + py - py * i_start] = pixel - 1;
                                    k7[k - 1 + py - py * i_start] = Mathf.Cos(teta[k * targetTexture.width + s1[k - 1 + py - py * i_start]]);
                                    k9[k - 1 + py - py * i_start] = Mathf.Cos(teta[k * targetTexture.width + pixel - 1]);
                                    while (G[k * targetTexture.width + pixel] < threshold1 && (pixel - s2[k - 1 + py - py * i_start]) < 25)
                                    {
                                        ++pixel;
                                    }
                                    if (pixel - s2[k - 1 + py - py * i_start] < 25)
                                    {
                                        while (G[k * targetTexture.width + pixel] > threshold1)
                                        {
                                            ++pixel;
                                        }
                                        s4[k - 1 + py - py * i_start] = pixel - 1;
                                        k7[k - 1 + py - py * i_start] = Mathf.Cos(teta[k * targetTexture.width + s1[k - 1 + py - py * i_start]]);
                                        k9[k - 1 + py - py * i_start] = Mathf.Cos(teta[k * targetTexture.width + pixel - 1]);
                                    }
                                    k2[k - 1 + py - py * i_start] = (s4[k - 1 + py - py * i_start] - s2[k - 1 + py - py * i_start]);
                                    for (int d = s2[k - 1 + py - py * i_start]; d <= s4[k - 1 + py - py * i_start]; ++d)
                                    {
                                        cols[k * targetTexture.width + d] = Color.red;
                                    }
                                }
                            }
                        }
                    }
                }
                if (PP[i * mbatch + j - 1] >= PP[i * mbatch + j] && PP[i * mbatch + j - 1] >= PP[i * mbatch + j + 1])
                {
                    --j;
                }
                else if (PP[i * mbatch + j + 1] >= PP[i * mbatch + j] && PP[i * mbatch + j + 1] >= PP[i * mbatch + j - 1])
                {
                    ++j;
                }
                for (int index = py * (i - 1) * targetTexture.width + px * (j - 2);
                    index <= py * (i - 1) * targetTexture.width + px * (j + 1) - 1; index++)
                {
                    cols[index] = Color.green;
                }
                for (int index = (py * i - 1) * targetTexture.width + px * (j - 2);
                 index <= (py * i - 1) * targetTexture.width + px * (j + 1) - 1; index++)
                {
                    cols[index] = Color.green;
                }
                for (int k = py * i - 1; k <= py * (i + 1) + 1; ++k)
                {
                    cols[k * targetTexture.width + px * (j - 2) + 1] = Color.green;
                    cols[k * targetTexture.width + px * (j + 1) - 2] = Color.green;
                    for (int l = px * (j - 2) - 1; l <= px * (j + 1) + 1; ++l)
                    {
                        values[k * targetTexture.width + l] = 0.587f * cols[k * targetTexture.width + l].g + 0.299f * cols[k * targetTexture.width + l].r +
                            0.114f * cols[k * targetTexture.width + l].b;
                    }
                }
                for (int k = py * i; k <= py * (i + 1) - 1; ++k)
                {
                    for (int l = px * (j - 2); l <= px * (j + 1) - 1; ++l)
                    {
                        float[] val = new float[8] { values[(k - 1) * targetTexture.width + l], values[(k + 1) * targetTexture.width + l], values[k * targetTexture.width + l - 1], values[k * targetTexture.width + l + 1], values[(k - 1) * targetTexture.width + l - 1], values[(k - 1) * targetTexture.width + l + 1], values[(k + 1) * targetTexture.width + l - 1], values[(k + 1) * targetTexture.width + l + 1] };
                        Array.Sort(val);
                        values[k * targetTexture.width + l] = (val[3] + val[4]) / 2;
                    }
                }
                for (int k = py * i; k <= py * (i + 1) - 1; ++k)
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
                            PP[(i + 1) * mbatch + j - 1]++;
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
                            PP[(i + 1) * mbatch + j]++;
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
                            PP[(i + 1) * mbatch + j + 1]++;
                        }
                    }
                }
                if (Mathf.Max(PP[(i + 1) * mbatch + j - 1], PP[(i + 1) * mbatch + j], PP[(i + 1) * mbatch + j + 1]) >= py * p_min)
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
                if (k2[x] < 35 && k2[x] > 2)
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
                AverageThicknessIn_mm = ((sum / (n)) * 0.4f) * Mathf.Pow((DTC - 0.07f), 1f);

                MaxThicknessIn_mm = (max * 0.4f) * Mathf.Pow((DTC - 0.07f), 1f);
                Length_mm = (n * 100 / (n2 - n1));
                Area = (DTC);
            }
        }
        targetTexture.SetPixels32(cols);
        targetTexture.Apply();
        rndr.material.mainTexture = targetTexture;
    }
}