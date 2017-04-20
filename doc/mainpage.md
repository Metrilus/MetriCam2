MetriCam 2 User and Developer Documentation
===========================================

Welcome to the MetriCam2 documentation. This page gives some general hints on using MetriCam2.

\authors Michael Balda <michael.balda@metrilus.de>
\authors Hannes Hofmann <hannes.hofmann@metrilus.de>

User Documentation
==================

If you want to use MetriCam2 to acquire images from a camera this part of the documentation is what you want to read.

[Useful Code Snippets](@ref page_code_snippets)

Camera Parameter Interface
--------------------------

MetriCam2 f&uuml;hrt einen Mechanismus ein, mit dem
* Kameraparameter &uuml;ber eine einheitliche Schnittstelle abgefragt und gesetzt werden k&ouml;nnen
* Kameraparameter annotiert werden k&ouml;nnen, um Informationen zum Typ/Range/Einheit etc. zu hinterlegen.

Dabei &uuml;bernimmt die MetriCam2.Camera Basisklasse die ganze Arbeit die zu Parametern geh&ouml;renden Properties zu finden, die Werte zu validieren, etc. Sie stellt dazu Get/Set Methoden zur Verf&uuml;gung, die im Folgenden n&auml;her beschrieben werden. Damit dieser automatische Mechanismus funktioniert, m&uuml;ssen alle Kameraparameter annotiert sein. Ohne Annotierung, als ganz normale Properties, ist ein manuelles Setzen nat&uuml;rlich weiterhin m&ouml;glich, aber unerw&uuml;nscht. 

### API ###

- MetriCam2.Camera.ParamDesc MetriCam2.Camera.GetParameter(string name)
- List<MetriCam2.Camera.ParamDesc> MetriCam2.Camera.GetParameters()
- void MetriCam2.Camera.SetParameter(string name, object value)
- void MetriCam2.Camera.SetParameters(Dictionary<string, object> NamesValues)

Channel Intrinsics
------------------

Die Methode MetriCam2.Camera.GetIntrinsics(string channelName) l&auml;dt intrinsische Kameraparameter nach folgendem Suchschema:
1. Suche _camName_camSerial_channelName.pt_ in
  - Lokal
  - Registry
  - Embedded Resource
2. Suche _camName_default_channelName.pt_ in
  - Lokal
  - Registry
  - Embedded Resource
3. Suche _camName_camSerial.pt_ in
  - Lokal
  - Registry
  - Embedded Resource
4. Suche _camName_default.pt_ in
  - Lokal
  - Registry
  - Embedded Resource

Die Suche endet mit dem ersten Treffer.

Channel Extrinsics
------------------

Aktuell muss die Datei *cam1Name_serial1_channel1_cam2Name_serial2_channel2.rbt* heissen. Sie wird zun&auml;chst lokal und dann im Registry-Pfad gesucht.

Sollte beides nicht gefunden werden, so wird die inverse RBT gesucht (also *cam2Name_serial2_channel2_cam1Name_serial1_channel1.rbt*).

Man bekommt immer 1 -> 2 zur&uuml;ck (wird also bei Bedarf invertiert).

Die aktuelle MetriCam2 API bietet nur die Methode *GetExtrinsics(string, string)* an.
Methoden um flexibler Extrinsics laden zu k&ouml;nnen (z.B. mit zwei verschiedenen Kameras) sind aber zumindest vorbereitet (private).
TODO: Die sollten wohl zumindest protected werden, damit eine kombinierte Kamera (RGBD oder TriggeredStereoCamera) sie verwenden kann. 


Developer Documentation
=======================

If you want to implement a wrapper for a yet unsupported camera, or understand how MetriCam2 works, and why, then this part of the documentation will provide you some insight.

* [Naming conventions](@ref page_dev_naming_conventions)
* [Adding a new camera wrapper](@ref page_dev_adding_a_new_camera)

Coding Guidelines
-----------------
* Every public property of a camera shall have a corresponding descriptor (see [Camera Parameter Annotations](@ref page_dev_parameter_annotations)).
* Parameter descriptors must not throw exceptions.