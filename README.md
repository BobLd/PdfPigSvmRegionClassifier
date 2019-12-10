# PdfPigSvmRegionClassifier
Proof of concept of an SVM Region Classifier using PdfPig and Accord.Net

# Model evaluation
## Accuracy
Model accuracy = 90.898

## Confusion matrix
| |title|text|list|table|image|
|---|---|---|---|---|---|
|**title**|9312|1592|19|3|135|
|**text**|1166|37136|988|820|32|
|**list**|0|1|32|0|0|
|**table**|0|16|4|1092|3|
|**image**|0|0|0|0|154|

## Precision, Recall and F1 score
**title**:
Precision: 0.842
Recall:    0.889
F1 score:  0.865

**text**:
Precision: 0.925
Recall:    0.958
F1 score:  0.941

**list**:
Precision: 0.970
Recall:    0.031
F1 score:  0.059

**table**:
Precision: 0.979
Recall:    0.570
F1 score:  0.721

**image**:
Precision: 1.000
Recall:    0.475
F1 score:  0.644

# References
- https://visualstudiomagazine.com/articles/2019/02/01/support-vector-machines.aspx
- http://accord-framework.net/docs/html/T_Accord_MachineLearning_Performance_GridSearch_2.htm
- https://github.com/ibm-aur-nlp/PubLayNet
