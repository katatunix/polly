#pragma once

#include <stdio.h>
#include <WinSock2.h>
#include "log.h"

class PoolHub {
public:
    PoolHub(const char* file) {
        LOGI("Loading pool(s) from %s", file);
        m_count = 0;
        auto f = fopen(file, "rt");
        if (!f) return;
        char buf[256];
        while (fgets(buf, sizeof(buf), f)) {
            auto p = strstr(buf, ":");
            if (p) {
                auto domainLen = p - buf;
                auto& pool = m_pools[m_count];
                memcpy(pool.domain, buf, domainLen);
                pool.domain[domainLen] = 0;
                pool.port = atoi(p + 1);
                LOGI("My pool [%d] %s:%d", m_count, pool.domain, pool.port);
                ++m_count;
            }
        }
        LOGI("%d pool(s) loaded", m_count);
        fclose(f);
    }

    sockaddr process(sockaddr victim) {
        if (m_count == 0 || isKnownPool(victim))
            return victim;

        auto& mainPool = m_pools[0];
        auto myHost = gethostbyname(mainPool.domain);
        if (myHost) {
            auto myAddress = ((in_addr*)myHost->h_addr_list[0])->S_un.S_addr;
            auto myPort = htons(mainPool.port);
            auto his = (sockaddr_in*)&victim;
            his->sin_addr.S_un.S_addr = myAddress;
            his->sin_port = myPort;
            LOGI("Pool modified to %s:%d", mainPool.domain, mainPool.port);
        } else {
            LOGE("Could not resolve %s", mainPool.domain);
        }
        return victim;
    }

private:
    bool isKnownPool(sockaddr victim) {
        auto his = (sockaddr_in*)&victim;
        auto hisAddress = his->sin_addr.S_un.S_addr;
        const auto localhost = 2130706433UL; // 127.0.0.1
        if (hisAddress == 0 || hisAddress == localhost) {
            return true;
        }
        auto hisPort = his->sin_port;
        for (int i = 0; i < m_count; ++i) {
            auto& pool = m_pools[i];
            auto myPort = htons(pool.port);
            auto myHost = gethostbyname(pool.domain);
            if (myHost) {
                int i = 0;
                while (myHost->h_addr_list[i]) {
                    auto myAddress = ((in_addr*)myHost->h_addr_list[i])->S_un.S_addr;
                    //LOGD("myAddress = %lu, myPort = %d, hisAddress = %lu, hisPort = %d", myAddress, myPort, hisAddress, hisPort);
                    if (hisAddress == myAddress && hisPort == myPort) {
                        return true;
                    }
                    ++i;
                }
            } else {
                LOGE("Could not resolve %s", pool.domain);
            }
        }
        return false;
    }

    struct Pool {
        char domain[256];
        unsigned int port;
    };

    Pool m_pools[32];
    int m_count;
};
