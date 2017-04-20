Naming Conventions {#page_dev_naming_conventions}
==================

Over time, several naming conventions emerged.
Please respect them in your code to make the use of MetriCam2 as consistent as possible.
If you think that one of these rules is nonsense let us know and we can discuss to replace it globally.

Channel Names
=============
Default channel names are defined in MetriCam2.ChannelNames. If you need more, add a CustomChannelNames class to your camera.

For most of the pre-defined channel names default Channels are created in the ChannelRegistry automatically.

Camera Properties
=================
* The camera property "Framerate" is to be written like that (i.e. lowercase "r").

@todo Should there be an equivalent to MetriCam2.ChannelNames for property names?

Project Names
=============
The project name (for a camera wrapper) should be the name of the camera vendor, e.g. _Bluetechnix_,<br>
or, if the library supports only a series of cameras: vendor plus series, e.g. _BluetechnixArgos_,<br>
or, if the library supports only a specific camera model: vendor plus model, e.g. _BluetechnixArgosP100_.
@todo It's still not clearly defined when to use the -Camera suffix in project/class names.
