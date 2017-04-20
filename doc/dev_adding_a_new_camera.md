Adding a New Camera Implementation {#page_dev_adding_a_new_camera}
==================================

<ol>
<li>Choose the name for your new project (see @ref page_dev_naming_conventions)</li>
<li>In VS, perform a "Get Latest Version" on the MetriCam2-folder in the Source Control Explorer.</li>
<li>Copy the <tt>CameraTemplate</tt> project folder (in Explorer, not in VS) and rename the new folder.</li>
<li>Remove the read-only attribute from the <tt>CameraTemplate.csproj</tt>, <tt>CameraTemplate.cs</tt>, and <tt>Properties/AssemblyInfo.cs</tt> files.</li>
<li>Open the new project (i.e. double-click the <tt>CameraTemplate.csproj</tt> file).
 <ol>
  <li>Rename the project and the <tt>CameraTemplate.cs</tt> file.</li>
  <li>"File" -> "Save All". Do save the .cs file and the project file, do not create a new solution.<li>
 </ol>
<li>Open the MetriCam2 solution and add the new project to it (in the <tt>Cameras</tt> solution folder).</li>
<li>Adjust assembly properties: Assembly name: MetriCam2.Cameras.<em>ProjectName</em>.</li>
<li>The <tt>ProjectName.cs</tt> contains details on what to change. Work through it.</li>
</ol>