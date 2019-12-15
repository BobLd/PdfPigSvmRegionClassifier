# PdfPig SVM Region Classifier
Proof of concept of a simple Support Vector Machine Region Classifier using [PdfPig](https://github.com/UglyToad/PdfPig) and [Accord.Net](https://github.com/accord-net/framework/). The model was trained on a subset of the [PubLayNet](https://github.com/ibm-aur-nlp/PubLayNet#getting-data) dataset. See their license [here](https://cdla.io/permissive-1-0/).

The objective is to classify each text block using machine learning in a pdf document page as either _title_, _text_, _list_, _table_ and _image_.

The annotions from the dataset (see sample [here](https://github.com/ibm-aur-nlp/PubLayNet/blob/master/examples/samples.json)) were converted to the [PAGE](https://github.com/PRImA-Research-Lab/PAGE-XML) xml format.
Images from the dataset were not used. We leverage the pdf document features directly instead.

# Labels 
Following the [PubLayNet](https://github.com/ibm-aur-nlp/PubLayNet) methodology, the following [categories](https://github.com/ibm-aur-nlp/PubLayNet/tree/master/pre-trained-models) are available:

|Label|id (svm)|
|---:|:---:|
|**title**|0|
|**text**|1|
|**list**|2|
|**table**|3|
|**image**|4|

# Features
## Text
- Character count
- Percentage of numeric characters
- Percentage of alphabetical characters
- Percentage of symbolic characters
- Percentage of bullet characters
- Average delta to average page glyph height

## Paths
- Path count
- Percentage of Bezier curve paths
- Percentage of horizontal paths
- Percentage of vertical paths
- Percentage of oblique paths

## Images
- Image count
- Average area covered by images

## Code
See the [`GenerateData`](https://github.com/BobLd/PdfPigSvmRegionClassifier/blob/master/PdfPigSvmRegionClassifier/GenerateData.cs) class to generate a csv file with the features, using the pdf documents, and their respective PageXml ground truth (one xml document per page). See the [`FeatureHelper`](https://github.com/BobLd/PdfPigSvmRegionClassifier/blob/master/PdfPigSvmRegionClassifier/FeatureHelper.cs) class to easily generate the features vector from a block.

# Results (in sample)
## Accuracy
Model accuracy = 90.898

## Confusion matrix

| |title|text|list|table|image|
|---:|:---:|:---:|:---:|:---:|:---:|
|**title**|9312|1592|19|3|135|
|**text**|1166|37136|988|820|32|
|**list**|0|1|32|0|0|
|**table**|0|16|4|1092|3|
|**image**|0|0|0|0|154|

## Precision, Recall and F1 score

| |Precision|Recall|F1 score|
|---|:---:|:---:|:---:|
|**title**|0.842|0.889|0.865|
|**text**|0.925|0.958|0.941|
|**list**|0.970|0.031|0.059|
|**table**|0.979|0.570|0.721|
|**image**|1.000|0.475|0.644|

## Code
See the [`Trainer`](https://github.com/BobLd/PdfPigSvmRegionClassifier/blob/master/PdfPigSvmRegionClassifier/Trainer.cs) class to **train** and **evaluate** the model.
After training, the SVM model will be saved as a Gzip.

# Usage
Once the training is finished, you can test the classification on a new pdf document by using either [DocstrumBoundingBoxes](https://github.com/UglyToad/PdfPig/blob/master/src/UglyToad.PdfPig/DocumentLayoutAnalysis/DocstrumBoundingBoxes.cs) or [RecursiveXYCut](https://github.com/UglyToad/PdfPig/blob/master/src/UglyToad.PdfPig/DocumentLayoutAnalysis/RecursiveXYCut.cs) to generate the text blocks, and then classify each block.
See [`SvmZoneClassifier`](https://github.com/BobLd/PdfPigSvmRegionClassifier/blob/master/PdfPigSvmRegionClassifier/SvmZoneClassifier.cs) for a demo implementation. The SVM trained model is available [here](https://github.com/BobLd/PdfPigSvmRegionClassifier/tree/master/PdfPigSvmRegionClassifier/model).

# References
- https://visualstudiomagazine.com/articles/2019/02/01/support-vector-machines.aspx
- http://accord-framework.net/docs/html/T_Accord_MachineLearning_Performance_GridSearch_2.htm
- https://github.com/ibm-aur-nlp/PubLayNet
