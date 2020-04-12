rm -dfr tmp
mkdir tmp

from=polly/build
cp -pv $from/*.dll tmp
cp -pv $from/*.exe tmp
cp -pv $from/*.config tmp
cp -pv readme.md tmp

from2=libs/bootstrap/x64
cp -pv $from2/Release/bootstrap.exe tmp
cp -pv $from2/Release/injected.dll tmp
cp -pv $from2/ReleaseNoDevFee/injectedNoDevFee.dll tmp
cp -pv build.sample/* tmp

ver=$1
rm -dfr build
mkdir build
cd tmp
zip ../build/polly.$ver.zip ./*
cd ..
rm -dfr tmp
