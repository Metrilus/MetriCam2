Code Snippets {#page_code_snippets}
=============

How to obtain a FloatImage
===

Our Framework-<code>FloatImage</code> has a c'tor which takes a <code>FloatCameraImage</code> as parameter. **Caveat:** The <code>FloatCameraImage</code> is destroyed in the conversion.
~~~{.cs}
FloatCameraImage camImg = cam.CalcChannel(ChannelNames.Distances).ToFloatCameraImage();
FloatImage fi = new FloatImage(ref camImg);
~~~

How to obtain a Bitmap
===

~~~{.cs}
ColorCameraImage camImg = (ColorCameraImage)cam.CalcChannel(ChannelNames.Color);
Bitmap bmp = camImg.Data;
~~~

How to load a Camera's intrinsic parameters (and convert them to a Framework type)
===

<code>Camera.GetIntrinsics</code> returns an object of type <code>Metrilus.Util.ProjectiveTransformationZhang</code>. If you need an object of type <code>MetriPrimitives.Transformations.ProjectiveTransformationZhang</code> instead, you currently have to create it yourself:
~~~{.cs}
Metrilus.Util.ProjectiveTransformationZhang utilPT = (Metrilus.Util.ProjectiveTransformationZhang)camera.GetIntrinsics(ChannelNames.Color);
MetriPrimitives.Transformations.ProjectiveTransformationZhang primPT = new MetriPrimitives.Transformations.ProjectiveTransformationZhang(utilPT.Width, utilPT.Height, utilPT.Fx, utilPT.Fy, utilPT.Cx, utilPT.Cy, utilPT.K1, utilPT.K2, utilPT.K3, utilPT.P1, utilPT.P2);
~~~
