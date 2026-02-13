# Debian-Specific Installation Instructions

OpenCV is available as expected in the Debian repositories, but not all of its
dependencies are available in modern debian in the versions expected by
opencvsharp.

Fortunately, most of the dependencies are available in older version of the debian
repositories, and we can configure apt to use those repositories as fallbacks.

Add `bullseye` as a fallback repository in your apt configuration (usually
`/etc/apt/sources.list.d/*`) something like this (this example shows a bookworm
system, but the same should apply to newer versions):

```
Types: deb deb-src
URIs: mirror+file:///etc/apt/mirrors/debian.list
Suites: bookworm bookworm-updates bookworm-backports bullseye bullseye-updates
Components: main

Types: deb deb-src
URIs: mirror+file:///etc/apt/mirrors/debian-security.list
Suites: bookworm-security bullseye-security
Components: main
```

Then, you can install the required dependencies with:

```bash
sudo apt install libtesseract4 liblept5 libgtk2.0-0 libavcodec58 libavformat58 libavutil56 libswscale5 libtiff5 libopenexr25
```

Unfortunately, `libjpeg8` is not available in debian repositories since about
2017, and the replacement libjpeg62-turbo seems to not be compatible with
opencvsharp. You can _however_ install `libjpeg8-turbo` from the Ubuntu Jammy
repository, which is compatible. On bullseye, the dependencies are installed by
default, but you may have to install them manually on other versions.

To install `libjpeg8-turbo` from the Ubuntu Jammy repository:
```bash
wget http://launchpadlibrarian.net/587202140/libjpeg-turbo8_2.1.2-0ubuntu1_amd64.deb
sudo dpkg -i libjpeg-turbo8_2.1.2-0ubuntu1_amd64.deb
```
