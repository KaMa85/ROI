ROI Version: Mixed Reality App for Defect Quantification
Overview
This Mixed Reality application focuses on defect quantification, utilizing Canny Edge Detection with horizontal pixel-level analysis.
An automatic Region of Interest (ROI) algorithm is integrated to improve processing time.

Development Stage
The application is in development and not final. It successfully implements defect detection and quantification but lacks perspective correction.

Algorithm Description
The NewMeasurement algorithm operates as follows:

It scans predefined rows (rowmax) and columns (m'), segmenting the image based on n/n’ and m/m’ ratios for detailed analysis.
For each segment, the Canny algorithm detects edges using tmin and tmax as thresholds.
If the processed pixel count (PP) in a segment exceeds a minimum threshold (pmin), indicating potential defects, the analysis extends to adjacent segments.
The algorithm then quantifies defect dimensions within these areas, accumulating crack dimension data.
Through iterative adjustments, it refines the focus based on edge intensity, ensuring efficient defect identification and dimension accumulation.
The process ends by returning the dimensions of detected defects.
Future Directions
Future updates will include perspective correction to align the application's output more closely with actual dimensions.

Using the Application
Set up your Mixed Reality environment as required.
Follow the repository setup instructions for application integration.
Calibration and testing with tmin and tmax parameters are recommended based on specific use cases.
This README outlines the application's functionality, its current stage of development, and the roadmap for future enhancements. 
The inclusion of an automatic ROI algorithm marks progress in the application’s development, aiming for improved efficiency in Mixed Reality defect detection and quantification.
