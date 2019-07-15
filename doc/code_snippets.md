Code Snippets {#page_code_snippets}
=============

How to obtain a FloatImage
===

Our Framework-<code>FloatImage</code> has a c'tor which takes a <code>FloatImage</code> as parameter. **Caveat:** The <code>FloatImage</code> is destroyed in the conversion.
~~~{.cs}
FloatImage camImg = cam.CalcChannel(ChannelNames.Distances).ToFloatImage();
FloatImage fi = new FloatImage(ref camImg);
~~~

How to obtain a Bitmap
===

~~~{.cs}
ColorImage camImg = (ColorImage)cam.CalcChannel(ChannelNames.Color);
Bitmap bmp = camImg.Data;
~~~

How to load a Camera's intrinsic parameters (and convert them to a Framework type)
===

<code>Camera.GetIntrinsics</code> returns an object of type <code>Metrilus.Util.ProjectiveTransformationRational</code>. If you need an object of type <code>MetriPrimitives.Transformations.ProjectiveTransformationRational</code> instead, you currently have to create it yourself:
~~~{.cs}
Metrilus.Util.ProjectiveTransformationRational utilPT = (Metrilus.Util.ProjectiveTransformationRational)camera.GetIntrinsics(ChannelNames.Color);
MetriPrimitives.Transformations.ProjectiveTransformationRational primPT = new MetriPrimitives.Transformations.ProjectiveTransformationRational(utilPT.Width, utilPT.Height, utilPT.Fx, utilPT.Fy, utilPT.Cx, utilPT.Cy, utilPT.K1, utilPT.K2, utilPT.K3, utilPT.P1, utilPT.P2);
~~~
