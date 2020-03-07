#pragma once

#include <stdio.h>

#define LOGI(...)		{ char buf[1024]; sprintf(buf, __VA_ARGS__); printf("NoDevFee: %s\n", buf); }
#define LOGW(...)		{ char buf[1024]; sprintf(buf, __VA_ARGS__); printf("NoDevFee: [WARN] %s\n", buf); }
#define LOGD(...)		{ char buf[1024]; sprintf(buf, __VA_ARGS__); printf("NoDevFee: [DBG] %s\n", buf); }
#define LOGE(...)		{ char buf[1024]; sprintf(buf, __VA_ARGS__); printf("NoDevFee: [ERR] %s\n", buf); }
