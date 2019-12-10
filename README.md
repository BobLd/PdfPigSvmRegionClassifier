# PdfPigSvmRegionClassifier
Proof of concept of an SVM Region Classifier using PdfPig and Accord.Net

# Model evaluation
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

# References
- https://visualstudiomagazine.com/articles/2019/02/01/support-vector-machines.aspx
- http://accord-framework.net/docs/html/T_Accord_MachineLearning_Performance_GridSearch_2.htm
- https://github.com/ibm-aur-nlp/PubLayNet
