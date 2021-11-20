#include <sys/sysctl.h>
extern "C" {
    uint64_t getSecondsSinceBoot(const char *path) {
        //return [SystemUtil uptime];
        struct timeval boottime;
        int mib[2] = {CTL_KERN, KERN_BOOTTIME};
        size_t size = sizeof(boottime);
        time_t now;
        time_t uptime = -1;
        
        (void)time(&now);
        
        if (sysctl(mib, 2, &boottime, &size, NULL, 0) != -1 && boottime.tv_sec != 0) {
            uptime = now - boottime.tv_sec;
        }
        return uptime;
    }
}
