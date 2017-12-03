from=polly/build
to=release/polly

cp -pv $from/FSharp.Core.dll $to
cp -pv $from/FSharp.Data.dll $to
cp -pv $from/NghiaBui.Common.dll $to
cp -pv $from/polly.exe $to
cp -pv $from/polly.exe.config $to
cp -pv readme.md $to
